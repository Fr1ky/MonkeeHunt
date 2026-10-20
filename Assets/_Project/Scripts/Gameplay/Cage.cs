using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// A placed cage (server-spawned). Holds at most one caged monkey. The bars have no colliders (poop
// from inside and bullets from outside pass through); a low base collider is what Interact aims at.
// - E by a hunter on an EMPTY cage: picks it back up into a cage slot with room.
// - E by a monkey on an occupied cage: instant unlock with this cage's key, otherwise the unlock
//   minigame (cancelled when out of reach or blocked).
// - Its key: held by the hunter who locked it, dropped on the ground (GroundKey) when the holder is
//   netted / caged / killed / gone, picked up by anyone. It only exists while a monkey is inside.
//   It drops where the holder last stood on solid ground, so a holder lost in a pit leaves it at the edge.
// - Becomes empty again when the occupant is freed or dies; the key then vanishes.
public class Cage : NetworkBehaviour, IInteractable {

    private static readonly List<Cage> all = new();

    // Where the caged body is held.
    [SerializeField]
    private Transform holdPoint;

    [SerializeField]
    private GameObject groundKeyPrefab;

    [SerializeField]
    private LayerMask groundLayers = ~0;

    [SerializeField, Range(1, DirectionMinigame.MaxLength)]
    private int unlockKeys = 6;

    [SerializeField, Min(0.5f)]
    private float unlockReach = 2.5f;

    [SerializeField]
    private SyncVar<bool> occupied = new(false);

    [SerializeField]
    private SyncVar<PlayerID> occupant = new();

    // True while a player holds the key (false while it lies on the ground or there is none).
    [SerializeField]
    private SyncVar<bool> keyHeld = new(false);

    [SerializeField]
    private SyncVar<PlayerID> keyHolder = new();

    // The unlock this machine is currently doing (one at a time).
    private static Cage activeUnlock;
    private static PlayerNetworkController activeUnlocker;

    // Server only.
    private NetTrap occupantServer;
    private KeyRing holderRing;
    private Vector3 lastHolderGround;
    private GroundKey groundKey;
    private bool retrieved;

    public bool IsOccupied => occupied.value;
    public PlayerID Occupant => occupant.value;
    public bool KeyHeld => keyHeld.value;
    public PlayerID KeyHolder => keyHolder.value;
    public Vector3 HoldPoint => holdPoint ? holdPoint.position : transform.position;

    public static Cage FindByOccupant(PlayerID player) {
        foreach (var cage in all) {
            if (cage && cage.occupied.value && cage.occupant.value == player) {
                return cage;
            }
        }
        return null;
    }

    // Server: how many cage keys this body carries.
    public static int KeysHeldBy(KeyRing ring) {
        var count = 0;
        foreach (var cage in all) {
            if (cage && cage.keyHeld.value && cage.holderRing == ring) {
                count++;
            }
        }
        return count;
    }

    // Server: drop every key this body carries where it last stood on solid ground.
    public static void DropKeysOf(KeyRing ring) {
        var index = 0;
        foreach (var cage in all) {
            if (cage && cage.keyHeld.value && cage.holderRing == ring) {
                // Spread several keys a little so they don't overlap.
                var offset = index == 0 ? Vector3.zero : Quaternion.Euler(0f, index * 120f, 0f) * Vector3.forward * 0.4f;
                cage.DropKeyServer(cage.lastHolderGround + offset);
                index++;
            }
        }
    }

    // Server: a head-rider takes every key the ridden hunter holds. Returns how many moved.
    public static int TransferKeysServer(KeyRing from, KeyRing to) {
        if (!from || !to || !to.owner.HasValue) {
            return 0;
        }
        var moved = 0;
        foreach (var cage in all) {
            if (cage && cage.isServer && cage.keyHeld.value && cage.holderRing == from) {
                cage.HandKeyTo(to);
                moved++;
            }
        }
        return moved;
    }

    private void Awake() {
        all.Add(this);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        all.Remove(this);
        if (activeUnlock == this) {
            CancelUnlock();
        }
    }

    // Server: put a monkey in this cage; the locking hunter holds the key.
    public void LockInServer(NetTrap monkey, KeyRing locker) {
        if (!isServer || occupied.value || !monkey || !monkey.owner.HasValue) {
            return;
        }
        occupantServer = monkey;
        occupant.value = monkey.owner.Value;
        occupied.value = true;
        monkey.SetState(NetState.Caged);
        if (locker) {
            HandKeyTo(locker);
        }
    }

    // Server: free the occupant (not netted). The key vanishes.
    public void FreeOccupantServer() {
        if (!isServer || !occupied.value) {
            return;
        }
        if (occupantServer && occupantServer.State == NetState.Caged) {
            occupantServer.Release();
        }
        ClearOccupant();
    }

    // Server: an empty cage is picked up (first request wins). The hunter's HunterCages tells its
    // owner, since this cage is gone before a reply from it could arrive.
    public bool TryTakeServer() {
        if (!isServer || occupied.value || retrieved) {
            return false;
        }
        retrieved = true;
        Despawn();
        return true;
    }

    // Server: someone picked up this cage's key from the ground (first request wins).
    public bool PickUpKeyServer(GroundKey key, KeyRing ring) {
        if (!isServer || !key || key != groundKey || !occupied.value || !ring || !ring.owner.HasValue) {
            return false;
        }
        groundKey = null;
        key.Despawn();
        HandKeyTo(ring);
        return true;
    }

    public void Interact(PlayerNetworkController player) {
        if (!occupied.value) {
            if (player.TryGetComponent(out HunterCages cages)) {
                cages.TryRetrieve(this);
            }
            return;
        }
        // Occupied: only monkeys free the one inside.
        if (activeUnlock || !player.owner.HasValue || !player.TryGetComponent(out PlayerInfo info)
            || info.Team != Team.Monkeys) {
            return;
        }
        if (keyHeld.value && keyHolder.value == player.owner.Value) {
            RequestUnlock(true);
            return;
        }
        var minigame = DirectionMinigame.Instance;
        if (!minigame || minigame.IsActive) {
            return;
        }
        activeUnlock = this;
        activeUnlocker = player;
        minigame.Begin("Unlocking...", unlockKeys, () => {
            activeUnlock = null;
            activeUnlocker = null;
            RequestUnlock(false);
        }, () => {
            activeUnlock = null;
            activeUnlocker = null;
        });
    }

    private void Update() {
        if (activeUnlock == this) {
            UpdateUnlock();
        }
        if (!isServer) {
            return;
        }
        // The occupant was freed some other way, or died in the cage: the cage is empty again.
        if (occupied.value && (!occupantServer || occupantServer.State != NetState.Caged)) {
            ClearOccupant();
        }
        if (keyHeld.value) {
            if (holderRing) {
                TrackHolderGround();
            } else {
                // The holder was killed or left: the key falls where they last stood.
                DropKeyServer(lastHolderGround);
            }
        }
    }

    // Server: remember the solid ground under the key holder (kept while they jump, climb or fall).
    // A head-rider sits too high for the probe, so it uses the ridden hunter's feet instead.
    private void TrackHolderGround() {
        var probe = holderRing.transform.position;
        if (holderRing.TryGetComponent(out MonkeyRide ride) && ride.IsRiding && ride.MountServer) {
            probe = ride.MountServer.transform.position;
        }
        if (Physics.Raycast(probe + Vector3.up * 0.5f, Vector3.down, out var hit, 2f,
                groundLayers, QueryTriggerInteraction.Ignore)) {
            lastHolderGround = hit.point;
        }
    }

    // Unlocker's machine: cancel when out of reach, blocked, or the cage was emptied another way.
    private void UpdateUnlock() {
        var unlocker = activeUnlocker;
        var tooFar = !unlocker || Vector3.Distance(unlocker.transform.position, transform.position) > unlockReach;
        if (tooFar || unlocker.ActionsBlocked || !occupied.value) {
            CancelUnlock();
        }
    }

    private static void CancelUnlock() {
        activeUnlock = null;
        activeUnlocker = null;
        var minigame = DirectionMinigame.Instance;
        if (minigame && minigame.IsActive) {
            minigame.Cancel();
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestUnlock(bool withKey, RPCInfo info = default) {
        // First valid success wins; an already emptied cage is ignored.
        if (!occupied.value || (withKey && !(keyHeld.value && keyHolder.value == info.sender))) {
            return;
        }
        FreeOccupantServer();
    }

    private void HandKeyTo(KeyRing ring) {
        holderRing = ring;
        lastHolderGround = ring.transform.position;
        TrackHolderGround();
        keyHolder.value = ring.owner.Value;
        keyHeld.value = true;
    }

    // Server: the key falls to the ground near the point.
    private void DropKeyServer(Vector3 point) {
        if (!keyHeld.value) {
            return;
        }
        keyHeld.value = false;
        holderRing = null;
        if (!groundKeyPrefab) {
            return;
        }
        if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var hit, 4f, groundLayers,
                QueryTriggerInteraction.Ignore)) {
            point = hit.point;
        }
        var go = UnityProxy.Instantiate(groundKeyPrefab, point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
            gameObject.scene);
        networkManager.Spawn(go);
        if (go.TryGetComponent(out groundKey)) {
            groundKey.SetCageServer(this);
        }
    }

    private void ClearOccupant() {
        occupantServer = null;
        occupied.value = false;
        // No monkey inside any more: its key is gone too.
        keyHeld.value = false;
        holderRing = null;
        if (groundKey) {
            groundKey.Despawn();
            groundKey = null;
        }
    }
}
