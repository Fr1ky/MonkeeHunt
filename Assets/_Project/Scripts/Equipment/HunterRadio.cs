using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Hunter's walkie-talkie. With a walkie slot selected, LMB opens the code list (shared minigame, code
// entry). Typing a group's arrow code uses one of the slot's calls and calls a supply drop where the
// hunter stands; WASD keeps walking meanwhile. Being blocked (net, sleep, trip, ride, menu), switching
// slot or LMB again cancels without using a call.
public class HunterRadio : NetworkBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private HunterLoadout loadout;

    // The item asset (codes, drop prefab, timings); the server spawns drops from it.
    [SerializeField]
    private WalkieTalkieDefinition definition;

    [SerializeField]
    private LayerMask groundLayers = ~0;

    [SerializeField]
    private string callTitle = "Call in supplies  (arrow keys)";

    private readonly List<int[]> rowCodes = new();
    private readonly List<string> rowLabels = new();
    private readonly List<Color> rowColors = new();

    // Owner: a call is being typed from this slot.
    private int callSlot = -1;

    public bool IsCalling => callSlot >= 0;

    private void Update() {
        if (!isOwner || !definition) {
            return;
        }
        var minigame = DirectionMinigame.Instance;
        if (IsCalling) {
            var stillOpen = minigame && minigame.IsCodeEntry;
            if (!stillOpen) {
                callSlot = -1;
            } else if (player.ActionsBlocked || loadout.SelectedSlot != callSlot || input.AttackPressed) {
                callSlot = -1;
                minigame.Cancel();
            }
            return;
        }
        if (player.ActionsBlocked || !input.AttackPressed || !minigame || minigame.IsActive) {
            return;
        }
        var slot = loadout.SelectedSlot;
        if (loadout.Selected is not WalkieTalkieDefinition || !loadout.IsEquipReady || loadout.WalkieCalls(slot) <= 0) {
            return;
        }
        OpenCall(minigame, slot);
    }

    private void OpenCall(DirectionMinigame minigame, int slot) {
        rowCodes.Clear();
        rowLabels.Clear();
        rowColors.Clear();
        foreach (var code in definition.codes) {
            rowCodes.Add(code.arrows);
            rowLabels.Add(code.label);
            rowColors.Add(code.color);
        }
        callSlot = slot;
        minigame.BeginCode(callTitle, rowCodes, rowLabels, rowColors, row => {
            var usedSlot = callSlot;
            callSlot = -1;
            if (row >= 0 && row < definition.codes.Length && loadout.TryUseWalkieCall(usedSlot)) {
                RequestDrop(definition.codes[row].group, GroundUnderMe());
            }
        }, () => callSlot = -1);
    }

    private Vector3 GroundUnderMe() {
        var mask = groundLayers & ~(1 << gameObject.layer);
        return Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 5f, mask,
            QueryTriggerInteraction.Ignore)
            ? hit.point
            : transform.position;
    }

    [ServerRpc]
    private void RequestDrop(SupplyGroup group, Vector3 point) {
        if (!definition || !definition.dropPrefab) {
            return;
        }
        var go = UnityProxy.Instantiate(definition.dropPrefab, point, Quaternion.identity, gameObject.scene);
        networkManager.Spawn(go);
        if (go.TryGetComponent(out SupplyDrop drop)) {
            drop.SetupServer(definition, group);
        }
    }

    private void OnDisable() {
        if (IsCalling) {
            callSlot = -1;
            var minigame = DirectionMinigame.Instance;
            if (minigame && minigame.IsCodeEntry) {
                minigame.Cancel();
            }
        }
    }
}
