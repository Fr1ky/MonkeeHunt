using UnityEngine;

// Moves the hand socket (and the held item in it) around its authored pose: a swing-in on switching,
// a punch on firing, a dip while reloading, sway behind the look and a walking bob.
// The owner sees all of it; everyone else sees the swing-in (the held slot is synced) and the fire
// punch (fire messages). Reload, look and walking are only known on the owner's machine.
public class ViewModelMotion : MonoBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private HunterLoadout loadout;

    [SerializeField]
    private HunterWeapons weapons;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerMovementController movement;

    [Header("Swing in")]
    [SerializeField]
    private float raiseDrop = 0.18f;

    [SerializeField]
    private float raiseTilt = 30f;

    [Header("Fire punch (strength 1 = rifle)")]
    [SerializeField]
    private float punchBack = 0.04f;

    [SerializeField]
    private float punchTilt = 4f;

    [SerializeField]
    private float punchRecoveryRate = 25f;

    [Header("Reload dip")]
    [SerializeField]
    private float reloadDrop = 0.08f;

    [SerializeField]
    private float reloadTilt = 20f;

    [SerializeField]
    private float reloadRoll = 15f;

    [SerializeField]
    private float reloadRate = 18f;

    [Header("Look sway (degrees)")]
    [SerializeField]
    private float swayPerPixel = 0.04f;

    [SerializeField]
    private float maxSway = 2.5f;

    [SerializeField]
    private float swayRate = 10f;

    [Header("Walking bob (at walking speed)")]
    [SerializeField]
    private float bobStride = 1.6f;

    [SerializeField]
    private float bobSide = 0.012f;

    [SerializeField]
    private float bobUp = 0.01f;

    [SerializeField]
    private float bobRate = 10f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float raiseStart = -10f;
    private float raiseDuration = 0.15f;
    private float punch;
    private float reload;
    private Vector2 sway;
    private float bobPhase;
    private float bobAmount;

    private void Awake() {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        if (loadout) {
            loadout.OnSelectionChanged += OnSelectionChanged;
            loadout.OnItemsChanged += Raise;
        }
    }

    private void OnDestroy() {
        if (loadout) {
            loadout.OnSelectionChanged -= OnSelectionChanged;
            loadout.OnItemsChanged -= Raise;
        }
    }

    // Fire punch; strength 1 = the rifle's.
    public void Punch(float strength) {
        punch = Mathf.Max(punch, strength);
    }

    private void OnSelectionChanged(int slot) {
        Raise();
    }

    private void Raise() {
        raiseStart = Time.time;
        var selected = loadout.Selected;
        raiseDuration = Mathf.Max(0.05f, selected ? selected.equipTime : 0.15f);
    }

    private void LateUpdate() {
        var dt = Time.deltaTime;
        punch = Mathf.Lerp(punch, 0f, 1f - Mathf.Exp(-punchRecoveryRate * dt));
        // Fast at first, settling softly at the end.
        var raiseLeft = 1f - Mathf.Clamp01((Time.time - raiseStart) / raiseDuration);
        var raise = raiseLeft * raiseLeft * raiseLeft;

        var owner = player && player.isOwner;
        var reloadTarget = owner && weapons && weapons.IsReloading ? 1f : 0f;
        reload = Mathf.Lerp(reload, reloadTarget, 1f - Mathf.Exp(-reloadRate * dt));

        var look = owner && input && !player.ControlsLocked ? input.Look : Vector2.zero;
        var swayTarget = Vector2.ClampMagnitude(-look * swayPerPixel, maxSway);
        sway = Vector2.Lerp(sway, swayTarget, 1f - Mathf.Exp(-swayRate * dt));

        var speed = owner && movement && movement.enabled && movement.IsGrounded ? movement.FlatSpeed : 0f;
        var walk = movement ? Mathf.Max(0.1f, movement.GroundSpeed(false)) : 1f;
        bobAmount = Mathf.Lerp(bobAmount, Mathf.Clamp(speed / walk, 0f, 1.6f), 1f - Mathf.Exp(-bobRate * dt));
        bobPhase = Mathf.Repeat(bobPhase + speed / bobStride * 2f * Mathf.PI * dt, 2f * Mathf.PI);
        // Side to side once per stride, down twice (each step).
        var bob = new Vector3(Mathf.Sin(bobPhase) * bobSide, (Mathf.Cos(2f * bobPhase) - 1f) * 0.5f * bobUp, 0f)
            * bobAmount;

        transform.localPosition = basePosition + bob
            + Vector3.down * (raiseDrop * raise + reloadDrop * reload)
            + Vector3.back * (punchBack * punch);
        // Positive pitch tips the item down; looking up makes it lag downwards.
        transform.localRotation = baseRotation * Quaternion.Euler(
            raiseTilt * raise + reloadTilt * reload - punchTilt * punch - sway.y,
            sway.x,
            reloadRoll * reload);
    }
}
