using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public enum RideExit : byte {
    // Clean jump off: the rider's Jump, or the ridden hunter went down.
    Hop,
    // Shaken or shot off: flung away, then lies tipped over until the trip time has passed.
    Thrown
}

// Monkey head-ride. Landing on top of a hunter's head (RideMount) grabs on: no snap or auto-aim, so it
// takes a precise jump or drop. One rider per hunter; hunters only. Landing on a monkey the hunter
// carries (it sits on the carrier's head) counts too.
// - The server holds the ride (riding + the hunter's player id, synced) and takes the first valid
//   mount request. Mounting makes a carrier drop its monkey and moves all the hunter's keys to the rider.
// - The rider's own machine glues its body (kinematic) to the hunter's seat, like carrying, and syncs a
//   steer direction (WASD relative to where it looks) that the hunter's machine mixes into its movement.
//   Rider: steer + look only; Jump hops off in the look direction.
// - Ends: Jump or the hunter going down (netted / asleep / tripped / dead) = hop off; the shake-off
//   minigame or any gun hit on the rider = thrown off (fling, then the peel-trip fall for 3 s).
// - Collisions between the rider and the ridden hunter are off while riding, on every machine.
public class MonkeyRide : NetworkBehaviour {

    private static readonly List<MonkeyRide> all = new();

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private Transform visualisation;

    [SerializeField]
    private MonkeyClimb climb;

    [SerializeField]
    private MonkeySwing swing;

    [SerializeField]
    private NetTrap net;

    [SerializeField]
    private TranqStatus tranq;

    [SerializeField]
    private TripStatus trip;

    [SerializeField]
    private PlayerHealth health;

    [SerializeField]
    private KeyRing keyRing;

    [Header("Mount check")]
    [SerializeField]
    private float probeRadius = 0.15f;

    // How far below the feet the probe reaches.
    [SerializeField]
    private float probeDistance = 0.25f;

    // The feet must be at most this far below the top of the head...
    [SerializeField]
    private float headTopTolerance = 0.15f;

    // ...and at most this far (horizontally) from the hunter's centre line.
    [SerializeField]
    private float headCentreTolerance = 0.3f;

    // Only while falling or level, not on the way up.
    [SerializeField]
    private float maxRiseSpeed = 0.5f;

    // Server sanity check: the monkey must be this close to the seat.
    [SerializeField]
    private float mountReach = 1.5f;

    [Header("Getting off")]
    [SerializeField]
    private float hopForwardSpeed = 4f;

    [SerializeField]
    private float hopUpSpeed = 3f;

    [SerializeField]
    private float hopAirSeconds = 0.35f;

    [SerializeField]
    private float throwAwaySpeed = 5f;

    [SerializeField]
    private float throwUpSpeed = 4f;

    [SerializeField]
    private float throwSpreadDegrees = 45f;

    // The movement controller brakes hard even in the air, so it stays off this long after letting go.
    [SerializeField]
    private float throwAirSeconds = 0.5f;

    [SerializeField]
    private SyncVar<bool> riding = new(false);

    [SerializeField]
    private SyncVar<PlayerID> mountPlayer = new();

    // Owner-driven: world-space steer direction (x, z), length 0-1.
    [SerializeField]
    private SyncVar<Vector2> steer = new(Vector2.zero, 0.05f, true);

    private readonly RaycastHit[] probeHits = new RaycastHit[8];
    private Collider[] colliders;
    private int probeMask;

    // Owner only.
    private bool seated;
    private RideMount seat;
    private float nextMountRequest;
    private float reenableMovementTime = -1f;

    // Server only.
    private RideMount mountServer;

    // Every peer: the hunter whose collisions with this body are switched off.
    private RideMount ignoredMount;

    public bool IsRiding => riding.value;
    public PlayerID MountPlayer => mountPlayer.value;
    public RideMount MountServer => mountServer;

    // Owner: the body is glued to a hunter's head right now.
    public bool IsSeated => seated;

    public Vector3 SteerWorld => new Vector3(steer.value.x, 0f, steer.value.y);

    // The monkey riding this hunter, if any.
    public static MonkeyRide FindRiderOf(PlayerID hunter) {
        foreach (var ride in all) {
            if (ride && ride.riding.value && ride.mountPlayer.value == hunter) {
                return ride;
            }
        }
        return null;
    }

    public static Collider[] SolidColliders(GameObject root) {
        var found = new List<Collider>();
        foreach (var col in root.GetComponentsInChildren<Collider>(true)) {
            if (!col.isTrigger) {
                found.Add(col);
            }
        }
        return found.ToArray();
    }

    private void Awake() {
        all.Add(this);
        colliders = SolidColliders(gameObject);
        // Bodies are on the Player layer; only another body can be a head to land on.
        probeMask = 1 << gameObject.layer;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        all.Remove(this);
        SetCollisionsIgnored(ignoredMount, false);
        ignoredMount = null;
    }

    private void Update() {
        UpdateCollisionIgnore();
        if (isServer) {
            UpdateServer();
        }
        if (!isOwner) {
            return;
        }
        if (reenableMovementTime > 0f && Time.time >= reenableMovementTime) {
            reenableMovementTime = -1f;
            if (!seated && !(climb && climb.IsClimbing) && !(swing && swing.IsSwinging)) {
                movement.enabled = true;
            }
        }
        if (!seated) {
            return;
        }
        var push = Vector2.zero;
        if (!player.ControlsLocked) {
            var move = input.Move;
            var world = Vector3.ClampMagnitude(Flat(visualisation.forward) * move.y + Flat(visualisation.right) * move.x, 1f);
            push = new Vector2(world.x, world.z);
            if (input.JumpPressed) {
                RequestHop();
            }
        }
        if ((steer.value - push).sqrMagnitude > 0.0001f) {
            steer.value = push;
        }
    }

    private void FixedUpdate() {
        if (isOwner && !seated) {
            TryMount();
        }
    }

    // Owner: keep the body on the hunter's head.
    private void LateUpdate() {
        if (!isOwner || !seated) {
            return;
        }
        if (!seat) {
            // The hunter vanished (died, left) before the server's message arrived.
            Unseat(RideExit.Hop);
            return;
        }
        Glue();
    }

    // Owner: landed on top of a hunter's head?
    private void TryMount() {
        if (riding.value || Time.time < nextMountRequest || body.isKinematic) {
            return;
        }
        if (player.ActionsBlocked || player.MovementLocked || player.StatusBlocksMovement) {
            return;
        }
        if ((climb && climb.IsClimbing) || (swing && swing.IsSwinging) || body.linearVelocity.y > maxRiseSpeed) {
            return;
        }
        var start = probeRadius + 0.05f;
        var origin = transform.position + Vector3.up * start;
        var count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, probeHits, start + probeDistance,
            probeMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) {
            var col = probeHits[i].collider;
            if (col.transform.IsChildOf(transform)) {
                continue;
            }
            var mount = FindMountFor(col);
            if (!mount || mount.Rider) {
                continue;
            }
            var feet = transform.position;
            var offCentre = Vector3.ProjectOnPlane(feet - mount.transform.position, Vector3.up).magnitude;
            if (feet.y < mount.HeadTopY - headTopTolerance || offCentre > headCentreTolerance) {
                continue;
            }
            nextMountRequest = Time.time + 0.5f;
            RequestMount(mount);
            return;
        }
    }

    // The hunter whose head this collider belongs to. A monkey being carried sits on top of its carrier's
    // head, so landing on it counts as landing on the carrier.
    private static RideMount FindMountFor(Collider col) {
        var mount = col.GetComponentInParent<RideMount>();
        if (mount) {
            return mount;
        }
        var carried = col.GetComponentInParent<NetTrap>();
        if (carried && carried.State == NetState.Carried && carried.owner.HasValue) {
            var carrier = HunterCarry.FindCarrierOf(carried.owner.Value);
            return carrier ? carrier.GetComponent<RideMount>() : null;
        }
        return null;
    }

    [ServerRpc]
    private void RequestMount(NetworkIdentity target) {
        var mount = target ? target.GetComponent<RideMount>() : null;
        if (!CanMountServer(mount)) {
            return;
        }
        mountServer = mount;
        mountPlayer.value = mount.owner.Value;
        riding.value = true;
        if (mount.TryGetComponent(out HunterCarry carry)) {
            carry.DropServer();
        }
        if (keyRing && mount.TryGetComponent(out KeyRing hunterRing)) {
            Cage.TransferKeysServer(hunterRing, keyRing);
        }
        Mounted(owner.Value, mount);
    }

    // Server: first valid request wins; both bodies must be up and free. (No same-owner check: a player
    // only ever has one body, and host tests use fake bodies that share the host's id.)
    private bool CanMountServer(RideMount mount) {
        if (!mount || !mount.owner.HasValue || !owner.HasValue) {
            return false;
        }
        if (riding.value || mount.Rider || mount.IsDownServer) {
            return false;
        }
        var monkeyDown = (net && net.IsTrapped) || (tranq && tranq.IsAsleep) || (trip && trip.IsTrippedServer)
                         || (health && health.IsDead) || (TryGetComponent(out QuicksandTrap sand) && sand.IsTrapped);
        return !monkeyDown && Vector3.Distance(transform.position, mount.SeatPoint) <= mountReach;
    }

    [TargetRpc]
    private void Mounted(PlayerID target, NetworkIdentity mountIdentity) {
        var mount = mountIdentity ? mountIdentity.GetComponent<RideMount>() : null;
        if (mount) {
            Seat(mount);
        }
    }

    [ServerRpc]
    private void RequestHop() {
        if (riding.value) {
            EndRideServer(RideExit.Hop);
        }
    }

    // Server: the shake-off finished or a gun hit the rider.
    public void ThrowOffServer() {
        if (isServer && isSpawned && riding.value) {
            EndRideServer(RideExit.Thrown);
        }
    }

    private void UpdateServer() {
        if (!riding.value) {
            return;
        }
        if (!mountServer || mountServer.IsDownServer) {
            EndRideServer(RideExit.Hop);
            return;
        }
        // Safety net: gun hits already throw the rider off before netting / sleeping it.
        if ((net && net.IsTrapped) || (tranq && tranq.IsAsleep)) {
            EndRideServer(RideExit.Thrown);
        }
    }

    private void EndRideServer(RideExit exit) {
        riding.value = false;
        mountServer = null;
        if (exit == RideExit.Thrown && trip) {
            trip.NoteDownServer();
        }
        if (owner.HasValue) {
            RideEnded(owner.Value, exit);
        }
    }

    [TargetRpc]
    private void RideEnded(PlayerID target, RideExit exit) {
        if (seated) {
            Unseat(exit);
        }
    }

    private void Seat(RideMount mount) {
        seat = mount;
        seated = true;
        reenableMovementTime = -1f;
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.Movement
            | PlayerNetworkController.StatusBlock.Actions | PlayerNetworkController.StatusBlock.Interact);
        Glue();
    }

    private void Glue() {
        var point = seat.SeatPoint;
        body.position = point;
        transform.position = point;
    }

    // Owner: let go of the head and jump / get flung off.
    private void Unseat(RideExit exit) {
        var mount = seat;
        seated = false;
        seat = null;
        player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.None);
        steer.value = Vector2.zero;
        body.isKinematic = false;

        Vector3 velocity;
        if (exit == RideExit.Thrown) {
            var away = mount ? -mount.Facing : -Flat(visualisation.forward);
            away = Quaternion.Euler(0f, Random.Range(-throwSpreadDegrees, throwSpreadDegrees), 0f) * away;
            velocity = away * throwAwaySpeed + Vector3.up * throwUpSpeed;
            if (trip) {
                trip.BeginLocal();
            }
        } else {
            velocity = Flat(visualisation.forward) * hopForwardSpeed + Vector3.up * hopUpSpeed;
        }
        body.linearVelocity = velocity;
        movement.enabled = false;
        reenableMovementTime = Time.time + (exit == RideExit.Thrown ? throwAirSeconds : hopAirSeconds);
    }

    // Every peer: no collisions between the rider and the hunter it rides.
    private void UpdateCollisionIgnore() {
        var wanted = riding.value ? RideMount.FindByPlayer(mountPlayer.value) : null;
        if (wanted == ignoredMount) {
            return;
        }
        SetCollisionsIgnored(ignoredMount, false);
        ignoredMount = wanted;
        SetCollisionsIgnored(wanted, true);
    }

    private void SetCollisionsIgnored(RideMount mount, bool ignore) {
        if (!mount || mount.Colliders == null) {
            return;
        }
        foreach (var own in colliders) {
            foreach (var other in mount.Colliders) {
                if (own && other) {
                    Physics.IgnoreCollision(own, other, ignore);
                }
            }
        }
    }

    private static Vector3 Flat(Vector3 direction) {
        var flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
    }

    private void OnDisable() {
        if (seated) {
            seated = false;
            seat = null;
            if (body) {
                body.isKinematic = false;
            }
            if (player) {
                player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.None);
            }
        }
        if (reenableMovementTime > 0f) {
            reenableMovementTime = -1f;
            if (player && player.isOwner && movement) {
                movement.enabled = true;
            }
        }
    }
}
