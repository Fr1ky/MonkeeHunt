using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Hunter picks up and carries netted monkeys.
// - E on a netted monkey (via NetTrap): asleep -> instant pickup; awake -> 8-key pickup minigame that
//   races the monkey's own escape. The server takes the first valid result.
// - Carrying: ~70% speed, no firing; G drops the monkey (still netted). The monkey's own machine
//   keeps its body glued to CarryPoint (NetTrap) and it keeps trying to escape.
// - The server drops the monkey automatically when the carrier is netted, asleep, tripped or killed,
//   and stops carrying when the monkey escapes or is gone.
public class HunterCarry : NetworkBehaviour {

    private static readonly List<HunterCarry> all = new();

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private NetTrap ownNet;

    [SerializeField]
    private TranqStatus ownTranq;

    [SerializeField]
    private PlayerHealth ownHealth;

    [SerializeField]
    private Transform visualisation;

    // How far behind the carrier a dropped / escaping monkey is put down.
    [SerializeField]
    private float dropDistance = 0.9f;

    // Where the carried monkey's body sits (local to this hunter's root).
    [SerializeField]
    private Vector3 carryOffset = new Vector3(0f, 1.4f, -0.35f);

    [SerializeField, Range(1, DirectionMinigame.MaxLength)]
    private int pickupKeys = 8;

    [SerializeField, Min(0.5f)]
    private float pickupReach = 2.5f;

    [SerializeField, Range(0.1f, 1f)]
    private float carrySpeedMultiplier = 0.7f;

    [SerializeField]
    private SyncVar<bool> carrying = new(false);

    [SerializeField]
    private SyncVar<PlayerID> carriedPlayer = new();

    // Server only: the carried monkey's net.
    private NetTrap carriedServer;

    // Owner only: pickup minigame in progress.
    private NetTrap pickupTarget;

    public bool IsCarrying => carrying.value;
    public PlayerID CarriedPlayer => carriedPlayer.value;
    public Vector3 CarryPoint => transform.TransformPoint(carryOffset);

    // Ground-level spot behind the carrier (facing comes from Visualisation; the root never rotates).
    public Vector3 DropPoint {
        get {
            var back = visualisation ? -Vector3.ProjectOnPlane(visualisation.forward, Vector3.up).normalized : Vector3.back;
            return transform.position + back * dropDistance;
        }
    }

    // The hunter currently carrying this player, if any.
    public static HunterCarry FindCarrierOf(PlayerID player) {
        foreach (var carry in all) {
            if (carry && carry.carrying.value && carry.carriedPlayer.value == player) {
                return carry;
            }
        }
        return null;
    }

    private void Awake() {
        all.Add(this);
        if (ownHealth) {
            // The body despawns right after dying, so drop the monkey on the death event itself.
            ownHealth.OnDied += OnCarrierDied;
        }
    }

    private void OnCarrierDied(PlayerHealth _) {
        DropServer();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        all.Remove(this);
        if (ownHealth) {
            ownHealth.OnDied -= OnCarrierDied;
        }
        if (pickupTarget) {
            CancelPickup();
        }
    }

    // Owner: E on a netted monkey (called by NetTrap.Interact).
    public void TryPickUp(NetTrap target) {
        if (!isOwner || carrying.value || pickupTarget || player.ActionsBlocked || InQuicksand(target)) {
            return;
        }
        if (target.TryGetComponent(out TranqStatus tranq) && tranq.IsAsleep) {
            RequestPickup(target);
            return;
        }
        var minigame = DirectionMinigame.Instance;
        if (!minigame || minigame.IsActive) {
            return;
        }
        pickupTarget = target;
        minigame.Begin("Picking up...", pickupKeys, () => {
            var done = pickupTarget;
            pickupTarget = null;
            if (done) {
                RequestPickup(done);
            }
        }, () => pickupTarget = null);
    }

    private void Update() {
        if (isServer) {
            UpdateServer();
        }
        if (!isOwner) {
            return;
        }
        movement.SetSpeedMultiplier(this, carrying.value ? carrySpeedMultiplier : 1f);

        if (pickupTarget) {
            var tooFar = Vector3.Distance(transform.position, pickupTarget.transform.position) > pickupReach;
            if (tooFar || player.ActionsBlocked || pickupTarget.State != NetState.Netted) {
                CancelPickup();
            }
        }

        if (carrying.value && input.DropPressed && !player.ActionsBlocked) {
            RequestDrop();
        }
    }

    private void CancelPickup() {
        pickupTarget = null;
        var minigame = DirectionMinigame.Instance;
        if (minigame && minigame.IsActive) {
            minigame.Cancel();
        }
    }

    // Server: stop carrying when the monkey is gone / freed; drop it when the carrier can't hold it.
    private void UpdateServer() {
        if (!carrying.value) {
            return;
        }
        if (!carriedServer || carriedServer.State != NetState.Carried) {
            StopCarrying();
            return;
        }
        var carrierDown = (ownNet && ownNet.IsTrapped) || (ownTranq && ownTranq.IsAsleep)
                          || (ownHealth && ownHealth.IsDead);
        if (carrierDown) {
            DropServer();
        }
    }

    [ServerRpc]
    private void RequestPickup(NetworkIdentity targetIdentity) {
        var target = targetIdentity ? targetIdentity.GetComponent<NetTrap>() : null;
        // First valid result wins: the monkey may already have escaped, or someone else picked it up.
        // A body stuck in quicksand can't be picked up.
        if (carrying.value || !target || target.State != NetState.Netted || !target.owner.HasValue || InQuicksand(target)) {
            return;
        }
        carriedServer = target;
        carriedPlayer.value = target.owner.Value;
        carrying.value = true;
        target.SetState(NetState.Carried);
    }

    private static bool InQuicksand(NetTrap target) {
        return target && target.TryGetComponent(out QuicksandTrap sand) && sand.IsTrapped;
    }

    [ServerRpc]
    private void RequestDrop() {
        DropServer();
    }

    // Server: put the monkey down, still netted (G, or the carrier was netted / slept / tripped / killed).
    public void DropServer() {
        if (!isServer || !carrying.value) {
            return;
        }
        if (carriedServer && carriedServer.State == NetState.Carried) {
            carriedServer.SetState(NetState.Netted);
        }
        StopCarrying();
    }

    // Server: used when caging (step 4) hands the monkey over to a cage.
    public NetTrap TakeCarriedServer() {
        if (!isServer || !carrying.value) {
            return null;
        }
        var monkey = carriedServer;
        StopCarrying();
        return monkey;
    }

    private void StopCarrying() {
        carriedServer = null;
        carrying.value = false;
    }

    private void OnDisable() {
        if (movement) {
            movement.SetSpeedMultiplier(this, 1f);
        }
    }
}
