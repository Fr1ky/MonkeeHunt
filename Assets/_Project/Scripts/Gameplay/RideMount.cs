using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// The hunter's head, as something a monkey can ride (MonkeyRide holds the ride state).
// - Gives the seat point on top of the head and the head's top for the mount check.
// - Owner, while ridden: move only (no firing, aiming, switching, reloading or E; wiping poop is
//   still allowed), the rider's steering is mixed into this body's movement (tug of war, rider
//   stronger), and the mouse-flick shake-off minigame runs; finishing it throws the rider off.
// - Server: tells the rider when this hunter is down (netted, asleep, tripped, dead).
public class RideMount : NetworkBehaviour {

    private static readonly List<RideMount> all = new();

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private NetTrap net;

    [SerializeField]
    private TranqStatus tranq;

    [SerializeField]
    private TripStatus trip;

    [SerializeField]
    private PlayerHealth health;

    // Facing comes from Visualisation (the root never rotates).
    [SerializeField]
    private Transform visualisation;

    // Where the rider's body sits (local to this hunter's root, which never rotates).
    // The capsule top is at 1.33 m; the seat leaves a 5 cm gap.
    [SerializeField]
    private Vector3 seatOffset = new Vector3(0f, 1.38f, 0f);

    // Height of the top of the head (capsule top) above the root.
    [SerializeField]
    private float headTop = 1.33f;

    // How much this hunter's own WASD counts against the rider's push (rider = 1).
    [SerializeField, Range(0f, 1f)]
    private float ownInputWeight = 0.6f;

    [SerializeField, Range(1, DirectionMinigame.MaxLength)]
    private int shakeOffKeys = 8;

    [SerializeField]
    private string shakeOffTitle = "Shake it off! (look that way, then back)";

    private Collider[] colliders;
    private bool shakeOffRunning;
    private float shakeOffCooldownUntil;

    public Vector3 SeatPoint => transform.TransformPoint(seatOffset);
    public float HeadTopY => transform.position.y + headTop;
    public Collider[] Colliders => colliders;

    // The hunter's head pivot (eyes / camera; pitches with the look). The rider's hands cover it.
    public Transform Head {
        get {
            if (!head && visualisation) {
                head = visualisation.Find("Head");
            }
            return head;
        }
    }

    private Transform head;

    // Flat direction the hunter faces.
    public Vector3 Facing {
        get {
            var flat = visualisation ? Vector3.ProjectOnPlane(visualisation.forward, Vector3.up) : Vector3.forward;
            return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        }
    }

    // The monkey riding this hunter, if any (every peer).
    public MonkeyRide Rider => owner.HasValue ? MonkeyRide.FindRiderOf(owner.Value) : null;

    // Server: this hunter can't carry a rider right now (the rider hops off).
    public bool IsDownServer => (net && net.IsTrapped) || (tranq && tranq.IsAsleep)
                                || (trip && trip.IsTrippedServer) || (health && health.IsDead)
                                || (sand && sand.IsTrapped);

    private QuicksandTrap sand;

    public static RideMount FindByPlayer(PlayerID player) {
        foreach (var mount in all) {
            if (mount && mount.owner.HasValue && mount.owner.Value == player) {
                return mount;
            }
        }
        return null;
    }

    private void Awake() {
        all.Add(this);
        colliders = MonkeyRide.SolidColliders(gameObject);
        TryGetComponent(out sand);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        all.Remove(this);
    }

    private void Update() {
        if (!isOwner) {
            return;
        }
        var rider = Rider;
        bool ridden = rider;
        player.SetStatusBlock(this, ridden
            ? PlayerNetworkController.StatusBlock.Actions | PlayerNetworkController.StatusBlock.Interact
              | PlayerNetworkController.StatusBlock.AllowWipe
            : PlayerNetworkController.StatusBlock.None);
        if (ridden) {
            movement.SetSteer(rider.SteerWorld, ownInputWeight);
        } else {
            movement.ClearSteer();
        }
        UpdateShakeOff(ridden);
    }

    // Owner: keep the shake-off going the whole time this hunter is ridden (like the net escape).
    private void UpdateShakeOff(bool ridden) {
        var minigame = DirectionMinigame.Instance;
        if (!minigame) {
            return;
        }
        var shouldRun = ridden && Time.time >= shakeOffCooldownUntil;
        if (shouldRun && !shakeOffRunning && !minigame.IsActive) {
            shakeOffRunning = true;
            minigame.Begin(shakeOffTitle, shakeOffKeys, () => {
                shakeOffRunning = false;
                shakeOffCooldownUntil = Time.time + 0.5f;
                RequestShakeOff();
            }, () => shakeOffRunning = false, DirectionMinigame.InputMode.MouseFlick);
        } else if (!shouldRun && shakeOffRunning) {
            minigame.Cancel();
        }
    }

    [ServerRpc]
    private void RequestShakeOff() {
        var rider = Rider;
        if (rider) {
            rider.ThrowOffServer();
        }
    }

    private void OnDisable() {
        if (movement) {
            movement.ClearSteer();
        }
        if (player) {
            player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.None);
        }
        if (shakeOffRunning) {
            shakeOffRunning = false;
            var minigame = DirectionMinigame.Instance;
            if (minigame && minigame.IsActive) {
                minigame.Cancel();
            }
        }
    }
}
