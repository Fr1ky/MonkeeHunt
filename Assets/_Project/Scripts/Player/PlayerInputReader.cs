using UnityEngine;
using UnityEngine.InputSystem;

// Reads the "Player" action map from the project's InputSystem_Actions asset.
// Each body works on its own runtime copy of the asset, so enabling/disabling one body's
// input (e.g. a remote player's) never touches the local player's actions.
// Only the local (owning) player enables it.
public class PlayerInputReader : MonoBehaviour {

    [SerializeField]
    private InputActionAsset actions;

    private InputActionAsset runtimeActions;
    private InputActionMap playerMap;
    private InputAction move;
    private InputAction look;
    private InputAction jump;
    private InputAction sprint;
    private InputAction interact;
    private InputAction attack;
    private InputAction reload;
    private InputAction aim;
    private InputAction scroll;
    private InputAction previous;
    private InputAction next;
    private InputAction eat;
    private InputAction wipe;
    private InputAction drop;
    private readonly InputAction[] arrows = new InputAction[4];
    private readonly InputAction[] slots = new InputAction[5];

    public Vector2 Move => playerMap.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => playerMap.enabled ? look.ReadValue<Vector2>() : Vector2.zero;
    public bool JumpPressed => playerMap.enabled && jump.WasPressedThisFrame();
    public bool SprintHeld => playerMap.enabled && sprint.IsPressed();
    // Fires on the key press itself, ignoring the Hold interaction set on the action.
    public bool InteractPressed => playerMap.enabled && interact.WasPressedThisFrame();
    // E is held down right now (quicksand help), also ignoring the Hold interaction.
    public bool InteractHeld => playerMap.enabled && interact.IsPressed();
    public bool AttackPressed => playerMap.enabled && attack.WasPressedThisFrame();
    public bool ReloadPressed => playerMap.enabled && reload.WasPressedThisFrame();
    public bool AimHeld => playerMap.enabled && aim.IsPressed();
    public bool EatPressed => playerMap.enabled && eat.WasPressedThisFrame();
    public bool WipeHeld => playerMap.enabled && wipe.IsPressed();
    public bool DropPressed => playerMap.enabled && drop.WasPressedThisFrame();

    // Arrow key pressed this frame for the direction minigame: 0 Up, 1 Right, 2 Down, 3 Left, or -1.
    public int DirectionPressed {
        get {
            if (!playerMap.enabled) {
                return -1;
            }
            for (int i = 0; i < arrows.Length; i++) {
                if (arrows[i].WasPressedThisFrame()) {
                    return i;
                }
            }
            return -1;
        }
    }
    public bool AttackHeld => playerMap.enabled && attack.IsPressed();
    public bool AttackReleased => playerMap.enabled && attack.WasReleasedThisFrame();

    // Slot index (0-4) pressed this frame via number keys, or -1.
    public int SlotPressed {
        get {
            if (!playerMap.enabled) {
                return -1;
            }
            for (int i = 0; i < slots.Length; i++) {
                if (slots[i].WasPressedThisFrame()) {
                    return i;
                }
            }
            return -1;
        }
    }

    // +1 wheel up / -1 wheel down / 0 this frame (mouse wheel only).
    public int ScrollStep {
        get {
            if (!playerMap.enabled) {
                return 0;
            }
            var wheel = scroll.ReadValue<float>();
            return wheel > 0.01f ? 1 : wheel < -0.01f ? -1 : 0;
        }
    }

    // -1 / 0 / +1 slot cycle step this frame (mouse wheel or gamepad d-pad Previous/Next).
    public int CycleStep {
        get {
            if (!playerMap.enabled) {
                return 0;
            }
            var wheel = scroll.ReadValue<float>();
            if (wheel > 0.01f || previous.WasPressedThisFrame()) {
                return -1;
            }
            if (wheel < -0.01f || next.WasPressedThisFrame()) {
                return 1;
            }
            return 0;
        }
    }

    private void Awake() {
        runtimeActions = Instantiate(actions);
        playerMap = runtimeActions.FindActionMap("Player", true);
        move = playerMap.FindAction("Move", true);
        look = playerMap.FindAction("Look", true);
        jump = playerMap.FindAction("Jump", true);
        sprint = playerMap.FindAction("Sprint", true);
        interact = playerMap.FindAction("Interact", true);
        attack = playerMap.FindAction("Attack", true);
        reload = playerMap.FindAction("Reload", true);
        aim = playerMap.FindAction("Aim", true);
        scroll = playerMap.FindAction("Scroll", true);
        previous = playerMap.FindAction("Previous", true);
        next = playerMap.FindAction("Next", true);
        eat = playerMap.FindAction("Eat", true);
        wipe = playerMap.FindAction("Wipe", true);
        drop = playerMap.FindAction("Drop", true);
        arrows[0] = playerMap.FindAction("ArrowUp", true);
        arrows[1] = playerMap.FindAction("ArrowRight", true);
        arrows[2] = playerMap.FindAction("ArrowDown", true);
        arrows[3] = playerMap.FindAction("ArrowLeft", true);
        for (int i = 0; i < slots.Length; i++) {
            slots[i] = playerMap.FindAction("Slot" + (i + 1), true);
        }
    }

    public void SetInputEnabled(bool enabled) {
        if (enabled) {
            playerMap.Enable();
        } else {
            playerMap.Disable();
        }
    }

    private void OnDisable() {
        playerMap?.Disable();
    }

    private void OnDestroy() {
        if (runtimeActions) {
            Destroy(runtimeActions);
        }
    }
}
