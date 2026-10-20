using UnityEngine;

// Local camera effects layered on top of mouse look: tranq "drunk" sway/roll and the field of view
// (base FOV, zoom from aiming / binoculars, drunk FOV wobble). Simple placeholder look for now.
public class PlayerCameraEffects : MonoBehaviour {

    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private TranqStatus tranq;

    [SerializeField]
    private float baseFov = 75f;

    [SerializeField]
    private float zoomSpeed = 12f;

    [Header("Drunk")]
    [SerializeField]
    private float maxRoll = 8f;

    [SerializeField]
    private float maxSway = 3f;

    [SerializeField]
    private float maxFovWobble = 5f;

    [SerializeField]
    private float levelSmoothing = 3f;

    private float drunkLevel;
    private float zoomFov;
    private float currentFov;

    // The player's field-of-view setting (Esc menu); the serialized value is only the fallback.
    public float BaseFov => LocalPlayerProfile.Fov > 1f ? LocalPlayerProfile.Fov : baseFov;

    // Current zoom as a fraction of the base FOV (1 = no zoom). Used to scale mouse sensitivity.
    public float ZoomRatio => currentFov / BaseFov;

    // 0 or less = no zoom.
    public void SetZoomFov(float fov) {
        zoomFov = fov;
    }

    private void OnEnable() {
        currentFov = BaseFov;
    }

    private void LateUpdate() {
        var target = tranq ? tranq.DrunkLevel : 0f;
        drunkLevel = Mathf.MoveTowards(drunkLevel, target, levelSmoothing * Time.deltaTime);

        var t = Time.time;
        var roll = Mathf.Sin(t * 1.3f) * maxRoll * drunkLevel;
        var swayYaw = Mathf.Sin(t * 0.9f) * maxSway * drunkLevel;
        var swayPitch = Mathf.Sin(t * 1.1f) * maxSway * 0.6f * drunkLevel;
        playerCamera.transform.localRotation = Quaternion.Euler(swayPitch, swayYaw, roll);

        var targetFov = zoomFov > 0f ? zoomFov : BaseFov;
        currentFov = Mathf.Lerp(currentFov, targetFov, 1f - Mathf.Exp(-zoomSpeed * Time.deltaTime));
        playerCamera.fieldOfView = currentFov + Mathf.Sin(t * 1.7f) * maxFovWobble * drunkLevel;
    }
}
