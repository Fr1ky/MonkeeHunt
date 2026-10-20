using PurrNet;
using UnityEngine;

// Hunter places cages. With a cage slot selected, left mouse places one on the ground in front:
// with the carried monkey inside (the hunter then holds its key), or empty.
// E on an empty cage (Cage.Interact) picks it back up into a cage slot with room.
public class HunterCages : NetworkBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private HunterLoadout loadout;

    [SerializeField]
    private HunterCarry carry;

    // Gets the key of a cage this hunter locks a monkey in.
    [SerializeField]
    private KeyRing keyRing;

    [SerializeField]
    private Transform visualisation;

    [SerializeField]
    private CageDefinition cageDefinition;

    [SerializeField]
    private LayerMask groundLayers = ~0;

    // Owner: one retrieve at a time, so a reply can't arrive for a slot that has since filled up.
    private bool retrievePending;

    // Owner: E on an empty cage. In the hub cages are never used up, so E just clears one away.
    public void TryRetrieve(Cage cage) {
        if (!isOwner || retrievePending || player.ActionsBlocked || (!loadout.HasCageRoom && !HunterLoadout.InHub)) {
            return;
        }
        retrievePending = true;
        RequestRetrieve(cage);
    }

    private void Update() {
        if (!isOwner || player.ActionsBlocked || !input.AttackPressed) {
            return;
        }
        if (loadout.Selected is not CageDefinition def || !loadout.IsEquipReady) {
            return;
        }
        if (!loadout.TryUseCage(loadout.SelectedSlot)) {
            return;
        }
        var forward = Vector3.ProjectOnPlane(visualisation.forward, Vector3.up).normalized;
        var point = transform.position + forward * def.placeDistance;
        if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out var hit, 6f, groundLayers,
                QueryTriggerInteraction.Ignore)) {
            point = hit.point;
        }
        RequestPlace(point, Quaternion.LookRotation(forward).eulerAngles.y);
    }

    [ServerRpc]
    private void RequestPlace(Vector3 point, float yaw) {
        if (!cageDefinition || !cageDefinition.cagePrefab) {
            return;
        }
        var go = UnityProxy.Instantiate(cageDefinition.cagePrefab, point, Quaternion.Euler(0f, yaw, 0f), gameObject.scene);
        networkManager.Spawn(go);
        if (!go.TryGetComponent(out Cage cage)) {
            return;
        }
        var monkey = carry ? carry.TakeCarriedServer() : null;
        if (monkey) {
            cage.LockInServer(monkey, keyRing);
        }
    }

    [ServerRpc]
    private void RequestRetrieve(NetworkIdentity cageIdentity) {
        var cage = cageIdentity ? cageIdentity.GetComponent<Cage>() : null;
        // First valid request wins: the cage may already be taken or have a monkey in it.
        var taken = cage && cage.TryTakeServer();
        if (owner.HasValue) {
            RetrieveResult(owner.Value, taken);
        }
    }

    [TargetRpc]
    private void RetrieveResult(PlayerID target, bool taken) {
        retrievePending = false;
        if (taken) {
            loadout.TryReturnCage();
        }
    }
}
