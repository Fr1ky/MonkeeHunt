using UnityEngine;

// Owner only. Hold Aim (right mouse):
// - gun: small fixed zoom;
// - item with adjustable zoom (binoculars, later scoped guns): starts at its default zoom each press,
//   mouse wheel zooms in / out within its range, mouse look slows by the zoom ratio.
// While aiming the wheel never switches slots (HunterLoadout ignores it).
// Binocular zoom also hides the held binoculars model (the binocular screen overlay takes its place).
public class HunterAim : MonoBehaviour {

    [SerializeField]
    private HunterLoadout loadout;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerCameraEffects cameraEffects;

    [SerializeField]
    private MouseAimController mouseAim;

    [SerializeField]
    private Transform viewSocket;

    // FOV divisor while aiming a gun without adjustable zoom (75 / 1.3 ~ 58).
    [SerializeField, Min(1f)]
    private float gunZoom = 1.3f;

    private float currentZoom;
    private bool wasAiming;

    public bool IsAiming { get; private set; }
    public bool IsBinocularsZoomed { get; private set; }

    // Current magnification while aiming (1 when not aiming).
    public float CurrentZoom => IsAiming || IsBinocularsZoomed ? currentZoom : 1f;

    private void Update() {
        if (!player.isOwner) {
            return;
        }
        var selected = loadout.Selected;
        // Only guns (small zoom) and adjustable-zoom items (binoculars) aim; cages and the walkie-talkie don't.
        var aimable = selected && (selected is WeaponDefinition || selected.adjustableZoom);
        var aiming = input.AimHeld && !player.ActionsBlocked && aimable && loadout.IsEquipReady;

        if (aiming && !wasAiming) {
            currentZoom = selected.adjustableZoom ? selected.defaultZoom : gunZoom;
        }
        wasAiming = aiming;

        if (aiming && selected.adjustableZoom) {
            var scroll = input.ScrollStep;
            if (scroll > 0) {
                currentZoom *= selected.zoomStep;
            } else if (scroll < 0) {
                currentZoom /= selected.zoomStep;
            }
            currentZoom = Mathf.Clamp(currentZoom, selected.minZoom, selected.maxZoom);
        }

        IsAiming = aiming && selected is WeaponDefinition;
        IsBinocularsZoomed = aiming && selected is BinocularsDefinition;

        cameraEffects.SetZoomFov(aiming ? cameraEffects.BaseFov / currentZoom : 0f);
        mouseAim.SetLookScale(aiming && selected.adjustableZoom ? cameraEffects.ZoomRatio : 1f);
        viewSocket.gameObject.SetActive(!IsBinocularsZoomed);
    }

    private void OnDisable() {
        IsAiming = IsBinocularsZoomed = false;
        wasAiming = false;
        if (cameraEffects) {
            cameraEffects.SetZoomFov(0f);
        }
        if (mouseAim) {
            mouseAim.SetLookScale(1f);
        }
        if (viewSocket) {
            viewSocket.gameObject.SetActive(true);
        }
    }
}
