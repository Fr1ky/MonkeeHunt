using PurrNet;
using UnityEngine;

// A rock / fruit / banana monkeys pick up with Interact (E). The server owns availability: first
// request wins, the spot hides, and it refills after respawnSeconds. The picker is told via TargetRpc.
public class PickupSpot : NetworkBehaviour, IInteractable {

    [SerializeField]
    private PickupType type;

    // Shown while available (also holds the collider Interact aims at).
    [SerializeField]
    private GameObject visual;

    [SerializeField, Min(1f)]
    private float respawnSeconds = 30f;

    [SerializeField]
    private SyncVar<bool> available = new(true);

    private float refillTime;

    public PickupType Type => type;
    public bool IsAvailable => available.value;

    private void Awake() {
        available.onChanged += AvailabilityChanged;
        AvailabilityChanged(available.value);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        available.onChanged -= AvailabilityChanged;
    }

    public void Interact(PlayerNetworkController player) {
        if (!available.value || !player.TryGetComponent(out MonkeyInventory inventory) || !inventory.CanPickUp(type)
            || inventory.PickupPending) {
            return;
        }
        inventory.BeginPickup();
        RequestPickup();
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestPickup(RPCInfo info = default) {
        if (!available.value) {
            DenyPickup(info.sender);
            return;
        }
        available.value = false;
        refillTime = Time.time + respawnSeconds;
        GrantPickup(info.sender, type);
    }

    [TargetRpc]
    private void GrantPickup(PlayerID target, PickupType pickupType) {
        var local = PlayerNetworkController.Local;
        if (local && local.TryGetComponent(out MonkeyInventory inventory)) {
            inventory.AddPickup(pickupType);
        }
    }

    // Someone else got there first: let the requester try again right away.
    [TargetRpc]
    private void DenyPickup(PlayerID target) {
        var local = PlayerNetworkController.Local;
        if (local && local.TryGetComponent(out MonkeyInventory inventory)) {
            inventory.EndPickup();
        }
    }

    private void Update() {
        if (isServer && !available.value && Time.time >= refillTime) {
            available.value = true;
        }
    }

    private void AvailabilityChanged(bool value) {
        if (visual) {
            visual.SetActive(value);
        }
    }
}
