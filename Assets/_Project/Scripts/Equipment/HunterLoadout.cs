using System;
using PurrNet;
using UnityEngine;

// Hunter's 5 slots. Any slot holds any item (each gun at most once, gear any number of times; see
// LoadoutCode), set on the hub racks and kept for the round. The owner switches with number keys /
// mouse wheel; the selected slot is synced so everyone sees the held model in the hand socket.
// - Round: empty slots are skipped; while carrying a monkey only cage slots can be selected.
// - Hub (no round running): empty slots can be selected so they can be filled. E on a rack item puts it
//   in the selected slot, G clears the slot (the server applies both). Nothing is used up in the hub.
public class HunterLoadout : NetworkBehaviour {

    public const int SlotCount = LoadoutCode.SlotCount;

    // Guns, in this order: hunting rifle, tranq gun, net launcher.
    [SerializeField]
    private WeaponDefinition[] weapons = new WeaponDefinition[3];

    [SerializeField]
    private BinocularsDefinition binoculars;

    [SerializeField]
    private CageDefinition cage;

    [SerializeField]
    private WalkieTalkieDefinition walkieTalkie;

    // While carrying a monkey only cage slots can be selected.
    [SerializeField]
    private HunterCarry carry;

    [SerializeField]
    private Transform viewSocket;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerNetworkController player;

    // Owner picks; others follow.
    [SerializeField]
    private SyncVar<int> selectedSlot = new(0, 0f, true);

    // The 5 items (LoadoutCode-packed). The server sets them: from the player's loadout when the body
    // spawns, and from rack picks in the hub.
    [SerializeField]
    private SyncVar<int> items = new(LoadoutCode.Default);

    private readonly EquipmentDefinition[] slots = new EquipmentDefinition[SlotCount];
    private readonly GameObject[] viewModels = new GameObject[SlotCount];

    // Owner-side cage stack per slot (like ammo).
    private readonly int[] cageCounts = new int[SlotCount];

    // Owner-side walkie-talkie calls left per slot.
    private readonly int[] walkieCalls = new int[SlotCount];
    private float equipReadyTime;

    // No round running: the lobby hub (racks, test range, nothing runs out, only dummies take hits).
    public static bool InHub => !RoundController.Instance;

    public int SelectedSlot => selectedSlot.value;
    public EquipmentDefinition Selected => slots[Mathf.Clamp(selectedSlot.value, 0, SlotCount - 1)];
    public bool IsEquipReady => Time.time >= equipReadyTime;
    public LoadoutItem ItemIn(int slot) => LoadoutCode.Get(items.value, slot);

    // Fires on every peer when the held slot changes.
    public event Action<int> OnSelectionChanged;

    // Fires on every peer when the items in the slots change.
    public event Action OnItemsChanged;

    // The held model built for a slot (null for empty slots).
    public GameObject ViewModel(int slot) => slot >= 0 && slot < SlotCount ? viewModels[slot] : null;

    public int CageCount(int slot) => slot >= 0 && slot < SlotCount ? cageCounts[slot] : 0;

    public int WalkieCalls(int slot) => slot >= 0 && slot < SlotCount ? walkieCalls[slot] : 0;

    // Owner: uses one call from the slot. False if it has none left. The hub never uses one up.
    public bool TryUseWalkieCall(int slot) {
        if (slot < 0 || slot >= SlotCount || walkieCalls[slot] <= 0) {
            return false;
        }
        if (!InHub) {
            walkieCalls[slot]--;
        }
        return true;
    }

    // Owner: every cage slot back to full (Capture supply crate).
    public void RefillCages() {
        for (int i = 0; i < SlotCount; i++) {
            if (slots[i] is CageDefinition def) {
                cageCounts[i] = def.perSlot;
            }
        }
    }

    // Owner: takes one cage from the slot. False if it has none. The hub never uses one up.
    public bool TryUseCage(int slot) {
        if (slot < 0 || slot >= SlotCount || cageCounts[slot] <= 0) {
            return false;
        }
        if (!InHub) {
            cageCounts[slot]--;
        }
        return true;
    }

    // A cage slot has room for one more cage.
    public bool HasCageRoom {
        get {
            for (int i = 0; i < SlotCount; i++) {
                if (slots[i] is CageDefinition def && cageCounts[i] < def.perSlot) {
                    return true;
                }
            }
            return false;
        }
    }

    // Owner: puts a picked-up cage back into the first cage slot with room. False if none.
    public bool TryReturnCage() {
        for (int i = 0; i < SlotCount; i++) {
            if (slots[i] is CageDefinition def && cageCounts[i] < def.perSlot) {
                cageCounts[i]++;
                return true;
            }
        }
        return false;
    }

    public EquipmentDefinition GetSlot(int index) {
        return index >= 0 && index < SlotCount ? slots[index] : null;
    }

    private void Awake() {
        selectedSlot.onChanged += SelectionChanged;
        items.onChanged += ItemsChanged;
        RebuildSlots();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        selectedSlot.onChanged -= SelectionChanged;
        items.onChanged -= ItemsChanged;
    }

    // Server only.
    public void SetItems(int packedItems) {
        if (isServer && LoadoutCode.IsValid(packedItems)) {
            items.value = packedItems;
        }
    }

    // Owner, hub only: put the item into the selected slot (Empty clears it). Saved on this PC right away;
    // the server applies the same rule (a gun already in another slot swaps places).
    public void TryPick(LoadoutItem item) {
        if (!isOwner || !InHub) {
            return;
        }
        var slot = selectedSlot.value;
        var picked = LoadoutCode.Assign(items.value, slot, item);
        if (!LoadoutCode.IsValid(picked) || picked == items.value) {
            return;
        }
        LocalPlayerProfile.Loadout = picked;
        LocalPlayerProfile.Save();
        RequestSetSlot(slot, item);
    }

    [ServerRpc]
    private void RequestSetSlot(int slot, LoadoutItem item) {
        if (!InHub || slot < 0 || slot >= SlotCount) {
            return;
        }
        var picked = LoadoutCode.Assign(items.value, slot, item);
        if (!LoadoutCode.IsValid(picked)) {
            return;
        }
        items.value = picked;
        if (owner.HasValue) {
            MatchSession.GetOrAdd(owner.Value).Loadout = picked;
        }
    }

    private void Update() {
        if (!isOwner || (player && player.ActionsBlocked)) {
            return;
        }
        if (InHub && input.DropPressed) {
            TryPick(LoadoutItem.Empty);
        }
        var pressed = input.SlotPressed;
        if (pressed >= 0) {
            Select(pressed);
            return;
        }
        // While aiming the wheel belongs to zoom (HunterAim), so it never switches slots.
        var step = input.AimHeld ? 0 : input.CycleStep;
        if (step != 0) {
            Select(NextSelectableSlot(selectedSlot.value, step));
        }
    }

    private void Select(int index) {
        if (index == selectedSlot.value || !Selectable(index)) {
            return;
        }
        selectedSlot.value = index;
    }

    private int NextSelectableSlot(int from, int step) {
        for (int i = 1; i <= SlotCount; i++) {
            var candidate = ((from + step * i) % SlotCount + SlotCount) % SlotCount;
            if (Selectable(candidate)) {
                return candidate;
            }
        }
        return from;
    }

    private bool Selectable(int index) {
        if (!slots[index]) {
            return InHub;
        }
        return !(carry && carry.IsCarrying) || slots[index] is CageDefinition;
    }

    private EquipmentDefinition ItemDefinition(LoadoutItem item) {
        return item switch {
            LoadoutItem.HuntingRifle => weapons.Length > 0 ? weapons[0] : null,
            LoadoutItem.TranqGun => weapons.Length > 1 ? weapons[1] : null,
            LoadoutItem.NetLauncher => weapons.Length > 2 ? weapons[2] : null,
            LoadoutItem.Binoculars => binoculars,
            LoadoutItem.Cage => cage,
            LoadoutItem.WalkieTalkie => walkieTalkie,
            _ => null
        };
    }

    private void RebuildSlots() {
        for (int i = 0; i < SlotCount; i++) {
            slots[i] = ItemDefinition(LoadoutCode.Get(items.value, i));
            cageCounts[i] = slots[i] is CageDefinition def ? def.perSlot : 0;
            walkieCalls[i] = slots[i] is WalkieTalkieDefinition walkie ? walkie.callsPerSlot : 0;
        }

        for (int i = 0; i < SlotCount; i++) {
            if (viewModels[i]) {
                Destroy(viewModels[i]);
                viewModels[i] = null;
            }
            if (slots[i] && slots[i].viewModelPrefab) {
                viewModels[i] = Instantiate(slots[i].viewModelPrefab, viewSocket, false);
                SetLayerRecursive(viewModels[i], gameObject.layer);
            }
        }
        // The held slot became unusable (e.g. a round body whose first slot is empty): take the first one that is.
        if (isOwner && !Selectable(Mathf.Clamp(selectedSlot.value, 0, SlotCount - 1))) {
            for (int i = 0; i < SlotCount; i++) {
                if (Selectable(i)) {
                    selectedSlot.value = i;
                    break;
                }
            }
        }
        ShowSelected();
    }

    private void ShowSelected() {
        for (int i = 0; i < SlotCount; i++) {
            if (viewModels[i]) {
                viewModels[i].SetActive(i == selectedSlot.value);
            }
        }
    }

    private void SelectionChanged(int index) {
        var selected = Selected;
        equipReadyTime = Time.time + (selected ? selected.equipTime : 0f);
        ShowSelected();
        OnSelectionChanged?.Invoke(index);
    }

    private void ItemsChanged(int packedItems) {
        RebuildSlots();
        // The held item may have changed even though the slot number didn't.
        var selected = Selected;
        equipReadyTime = Time.time + (selected ? selected.equipTime : 0f);
        OnItemsChanged?.Invoke();
    }

    private static void SetLayerRecursive(GameObject go, int layer) {
        go.layer = layer;
        foreach (Transform child in go.transform) {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
