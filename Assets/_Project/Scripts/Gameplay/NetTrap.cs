using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public enum NetState : byte {
    Free,
    Netted,
    Carried,
    Caged
}

// Net / capture state for one body (server-held, synced): Free -> Netted -> Carried -> Caged.
// - Netted or carried and awake: the owner keeps playing the escape minigame; finishing it frees them.
// - A teammate looks at a netted player and presses E: rescue minigame on the teammate's machine;
//   cancelled when they leave reach, get a status effect, or the target is freed another way.
// - E on a caged body goes to its cage (unlocking, Cage).
// - Netted / carried = can only look around (no moving, firing, switching or interacting).
// - F8 still frees yourself in the editor / development builds.
public class NetTrap : NetworkBehaviour, INetTarget, IInteractable {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInfo info;

    [SerializeField]
    private TranqStatus tranq;

    [SerializeField]
    private GameObject netVisual;

    [SerializeField]
    private Rigidbody body;

    [SerializeField, Range(1, DirectionMinigame.MaxLength)]
    private int escapeKeys = 6;

    [SerializeField, Range(1, DirectionMinigame.MaxLength)]
    private int rescueKeys = 5;

    [SerializeField, Min(0.5f)]
    private float rescueReach = 2.5f;

    [SerializeField]
    private SyncVar<NetState> state = new(NetState.Free);

    // The rescue this machine is currently doing (one at a time).
    private static NetTrap activeRescue;
    private static PlayerNetworkController activeRescuer;

    private bool escapeRunning;
    private bool gluedToCarrier;
    private Vector3 lastDropPoint;

    // After finishing, wait for the server's "freed" before starting another escape.
    private float escapeCooldownUntil;

    public NetState State => state.value;
    public bool IsTrapped => state.value != NetState.Free;

    // Netted or carried: still inside the net and able to escape.
    public bool IsInNet => state.value == NetState.Netted || state.value == NetState.Carried;

    private QuicksandTrap sand;

    private void Awake() {
        TryGetComponent(out sand);
        state.onChanged += StateChanged;
        StateChanged(state.value);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        state.onChanged -= StateChanged;
        if (activeRescue == this) {
            CancelRescue();
        }
    }

    // Server: net hit.
    public void TrapInNet() {
        if (isServer && state.value == NetState.Free) {
            state.value = NetState.Netted;
        }
    }

    // Server: back to free (escape, rescue, debug).
    public void Release() {
        if (isServer) {
            state.value = NetState.Free;
        }
    }

    // Server: used by carrying / caging (M5 steps 3-4).
    public void SetState(NetState newState) {
        if (isServer) {
            state.value = newState;
        }
    }

    // Look + E on this body: a hunter picks up a netted monkey; a teammate starts a rescue.
    // Aiming at a caged body counts as aiming at its cage.
    public void Interact(PlayerNetworkController interactor) {
        if (interactor == player) {
            return;
        }
        // Stuck in quicksand: a teammate's E starts pulling them out (it frees the net too).
        if (sand && sand.IsTrapped && sand.TryBeginHelp(interactor)) {
            return;
        }
        if (state.value == NetState.Caged) {
            var cage = owner.HasValue ? Cage.FindByOccupant(owner.Value) : null;
            if (cage) {
                cage.Interact(interactor);
            }
            return;
        }
        if (state.value != NetState.Netted || activeRescue) {
            return;
        }
        var interactorInfo = interactor.GetComponent<PlayerInfo>();
        if (!interactorInfo || !info) {
            return;
        }
        if (interactorInfo.Team == Team.Hunters && info.Team == Team.Monkeys
            && interactor.TryGetComponent(out HunterCarry carry)) {
            carry.TryPickUp(this);
            return;
        }
        if (interactorInfo.Team != info.Team) {
            return;
        }
        var minigame = DirectionMinigame.Instance;
        if (!minigame || minigame.IsActive) {
            return;
        }
        activeRescue = this;
        activeRescuer = interactor;
        minigame.Begin("Rescuing...", rescueKeys, () => {
            activeRescue = null;
            activeRescuer = null;
            RequestRescue();
        }, () => {
            activeRescue = null;
            activeRescuer = null;
        });
    }

    private void Update() {
        if (activeRescue == this) {
            UpdateRescue();
        }
        if (isServer && state.value == NetState.Carried && owner.HasValue && !HunterCarry.FindCarrierOf(owner.Value)) {
            // Safety net: the carrier vanished without dropping us (e.g. disconnected).
            state.value = NetState.Netted;
        }
        if (!isOwner) {
            return;
        }

        // In the net: look only. Caged: look, throw poop and eat (no moving, no E-interactions).
        player.SetStatusBlock(this, IsInNet
            ? PlayerNetworkController.StatusBlock.Movement | PlayerNetworkController.StatusBlock.Actions
            : state.value == NetState.Caged
                ? PlayerNetworkController.StatusBlock.Movement | PlayerNetworkController.StatusBlock.Interact
                : PlayerNetworkController.StatusBlock.None);

        UpdateEscape();

        if (IsInNet && DebugFreeAllowed && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) {
            RequestDebugRelease();
        }
    }

    // Owner: while carried the body rides at the carrier's carry point; while caged it is held in the
    // cage. Both kinematic.
    private void LateUpdate() {
        if (!isOwner || !body) {
            return;
        }
        var carrier = state.value == NetState.Carried && owner.HasValue ? HunterCarry.FindCarrierOf(owner.Value) : null;
        var cage = state.value == NetState.Caged && owner.HasValue ? Cage.FindByOccupant(owner.Value) : null;
        if (carrier || cage) {
            if (!gluedToCarrier) {
                gluedToCarrier = true;
                body.isKinematic = true;
            }
            var point = carrier ? carrier.CarryPoint : cage.HoldPoint;
            body.position = point;
            transform.position = point;
            // Let go behind the carrier; a freed caged monkey just stands in the open cage.
            lastDropPoint = carrier ? carrier.DropPoint : point;
        } else if (gluedToCarrier && state.value != NetState.Caged) {
            // Dropped, escaped or freed: put the body down (behind the carrier, not on their head).
            gluedToCarrier = false;
            body.position = lastDropPoint;
            transform.position = lastDropPoint;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
        }
    }

    // Owner: keep the escape minigame going while in the net and awake.
    private void UpdateEscape() {
        var minigame = DirectionMinigame.Instance;
        if (!minigame) {
            return;
        }
        var shouldRun = IsInNet && !(tranq && tranq.IsAsleep) && Time.time >= escapeCooldownUntil;
        if (shouldRun && !escapeRunning && !minigame.IsActive) {
            escapeRunning = true;
            minigame.Begin("Escape!", escapeKeys, () => {
                escapeRunning = false;
                escapeCooldownUntil = Time.time + 0.5f;
                RequestEscape();
            }, () => escapeRunning = false);
        } else if (!shouldRun && escapeRunning) {
            minigame.Cancel();
        }
    }

    // Rescuer's machine: cancel when out of reach, blocked, or the target is no longer netted.
    private void UpdateRescue() {
        var rescuer = activeRescuer;
        var tooFar = !rescuer || Vector3.Distance(rescuer.transform.position, transform.position) > rescueReach;
        if (tooFar || rescuer.ActionsBlocked || state.value != NetState.Netted) {
            CancelRescue();
        }
    }

    private static void CancelRescue() {
        activeRescue = null;
        activeRescuer = null;
        var minigame = DirectionMinigame.Instance;
        if (minigame && minigame.IsActive) {
            minigame.Cancel();
        }
    }

    [ServerRpc]
    private void RequestEscape() {
        if (IsInNet) {
            Release();
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestRescue() {
        // First valid success wins; a target already freed / picked up is ignored.
        if (state.value == NetState.Netted) {
            Release();
        }
    }

    private static bool DebugFreeAllowed => Application.isEditor || Debug.isDebugBuild;

    [ServerRpc]
    private void RequestDebugRelease() {
        if (DebugFreeAllowed) {
            Release();
        }
    }

    private void StateChanged(NetState value) {
        if (netVisual) {
            netVisual.SetActive(value == NetState.Netted || value == NetState.Carried);
        }
    }
}
