using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Turns a spawned player into either the local (owned) player or a remote copy.
// Local: input, movement, mouse look, interaction and camera on; own body renders shadows only.
// Remote: everything local-only is off and the rigidbody is kinematic,
// position/rotation come from the NetworkTransforms.
public class PlayerNetworkController : NetworkBehaviour {

    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private MouseAimController mouseAim;

    [SerializeField]
    private PlayerInteractor interactor;

    [SerializeField]
    private GameObject cameraRoot;

    [SerializeField]
    private Renderer[] hiddenForOwner;

    private static readonly List<PlayerNetworkController> all = new();

    // The spawned body this machine owns, if any. Looked up live rather than cached, so spawn /
    // ownership / despawn callback order (scene changes, other players joining) can never leave it stale.
    public static PlayerNetworkController Local {
        get {
            foreach (var player in all) {
                if (player && player.isSpawned && player.isOwner) {
                    return player;
                }
            }
            return null;
        }
    }

    private void Awake() {
        all.Add(this);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        all.Remove(this);
    }

    protected override void OnSpawned() {
        ApplyOwnership();
    }

    // True during the round countdown (movement frozen, look free).
    public bool CountdownLocked { get; private set; }

    // Movement is frozen by the countdown or an open menu (status effects are separate).
    public bool MovementLocked => CountdownLocked || ControlsLocked;

    // Local player only: freezes movement but keeps look free (round countdown).
    // Kept separate from the menu lock so one never clears the other.
    public void SetMovementLocked(bool locked) {
        if (!isOwner) {
            return;
        }
        CountdownLocked = locked;
        movement.LockMovement(MovementLocked);
    }

    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer) {
        ApplyOwnership();
    }

    // True while a menu (Esc, host console, loadout station) has taken control from this body.
    public bool ControlsLocked { get; private set; }

    // Status effects (tranq sleep, net) block parts of control. Several can be active at once.
    [System.Flags]
    public enum StatusBlock {
        None = 0,
        Movement = 1,
        Look = 2,
        Actions = 4,
        // No E-interactions (pickups, consoles...) but other actions still allowed (e.g. caged monkey).
        Interact = 8,
        // Combined with Actions: this source still lets the player wipe poop (X), e.g. being head-ridden.
        AllowWipe = 16
    }

    private readonly Dictionary<object, StatusBlock> statusBlocks = new();

    // Firing, switching and interacting are not allowed (menu open or a status effect).
    public bool ActionsBlocked => ControlsLocked || HasStatusBlock(StatusBlock.Actions);

    // A status effect (net, sleep, topple) holds this body in place.
    public bool StatusBlocksMovement => HasStatusBlock(StatusBlock.Movement);

    // Wiping poop (X) is blocked like other actions, except by sources that allow it (being ridden).
    public bool WipeBlocked {
        get {
            if (ControlsLocked) {
                return true;
            }
            foreach (var flags in statusBlocks.Values) {
                if ((flags & StatusBlock.Actions) != 0 && (flags & StatusBlock.AllowWipe) == 0) {
                    return true;
                }
            }
            return false;
        }
    }

    public void SetStatusBlock(object source, StatusBlock flags) {
        if (flags == StatusBlock.None) {
            statusBlocks.Remove(source);
        } else {
            statusBlocks[source] = flags;
        }
        movement.SetBlocked(HasStatusBlock(StatusBlock.Movement));
        mouseAim.SetBlocked(HasStatusBlock(StatusBlock.Look));
        interactor.enabled = isOwner && !ActionsBlocked && !HasStatusBlock(StatusBlock.Interact);
    }

    private bool HasStatusBlock(StatusBlock flag) {
        foreach (var flags in statusBlocks.Values) {
            if ((flags & flag) != 0) {
                return true;
            }
        }
        return false;
    }

    // Local player only: freezes movement, look and interaction and frees the cursor (for menus).
    public void SetControlLocked(bool locked) {
        if (!isOwner) {
            return;
        }
        ControlsLocked = locked;
        movement.LockMovement(MovementLocked);
        mouseAim.LockCamera(locked);
        interactor.enabled = !ActionsBlocked && !HasStatusBlock(StatusBlock.Interact);
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    private void ApplyOwnership() {
        var local = isOwner;
        if (local) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        input.SetInputEnabled(local);
        movement.enabled = local;
        mouseAim.enabled = local;
        interactor.enabled = local;
        cameraRoot.SetActive(local);
        body.isKinematic = !local;

        var shadowMode = local
            ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
            : UnityEngine.Rendering.ShadowCastingMode.On;
        foreach (var r in hiddenForOwner) {
            r.shadowCastingMode = shadowMode;
        }
    }
}
