using UnityEngine;

// Ported from 3dPixelZombies (fpsTesting branch), trimmed to mouse look only.
// Removed: headbob, camera tilt, recoil, item holder sway, aim obstruction marker, GameManager hooks.
// Pitch is applied to the head pivot instead of the camera so it can be networked.
public class MouseAimController : MonoBehaviour {

    [SerializeField] private Transform characterVisualisationRoot;

    // Pitches up/down. Networked so other players see where this player aims; the camera sits under it.
    [SerializeField]
    private Transform head;

    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private PlayerInputReader input;

    // Degrees per pixel of mouse movement. The original used the legacy "Mouse X" axis
    // (0.1 * pixel delta) * 90 * deltaTime, which made sensitivity depend on frame rate;
    // 0.15 matches it at 60 fps.
    [SerializeField]
    private float lookSensitivity = 0.15f;

    // Recoil / flinch: the view snaps up by the kick and springs back to where the player aimed.
    // Rates are per second (exponential approach): rise ~0.05 s, recovery ~0.3 s.
    [SerializeField]
    private float kickRiseRate = 60f;

    [SerializeField]
    private float kickRecoveryRate = 9f;

    private float xRotation = 0f;

    // Degrees up: the target springs back to 0, the applied offset chases it.
    private float kickTarget;
    private float kickOffset;

    private bool locked;

    // Set by status effects (tranq sleep), separate from the menu lock.
    private bool blocked;

    private Vector2 mouseSensitivity = new Vector2(0, 0);

    // Extra multiplier while zoomed (binoculars), 1 = normal.
    private float lookScale = 1f;

    public Camera PlayerCamera => playerCamera;

    public void LockCamera(bool locked) {
        this.locked = locked;
    }

    public void SetBlocked(bool blocked) {
        this.blocked = blocked;
    }

    public void UpdateFOV(float newFOV) {
        playerCamera.fieldOfView = newFOV;
    }

    public void UpdateMouseSensitivity(Vector2 newSensitivity) {
        mouseSensitivity = newSensitivity;
    }

    public void SetLookScale(float scale) {
        lookScale = Mathf.Max(0.01f, scale);
    }

    // Upward view kick (gun recoil, rock flinch) that recovers on its own.
    public void Kick(float degrees) {
        kickTarget += degrees;
    }

    public Ray GetCameraRay() {
        return playerCamera.ViewportPointToRay(new Vector3(0.5f, .5f, 0));
    }

    void LateUpdate() {
        var dt = Time.deltaTime;
        var kicking = kickTarget > 0.001f || kickOffset > 0.001f;
        if (kicking) {
            kickOffset = Mathf.Lerp(kickOffset, kickTarget, 1f - Mathf.Exp(-kickRiseRate * dt));
            kickTarget = Mathf.Lerp(kickTarget, 0f, 1f - Mathf.Exp(-kickRecoveryRate * dt));
        } else {
            kickTarget = kickOffset = 0f;
        }

        if (locked || blocked) {
            // Look is frozen, but a kick in progress still plays out and recovers.
            if (kicking) {
                ApplyPitch();
            }
            return;
        }

        var look = input.Look;
        // LocalPlayerProfile.Sensitivity is the player's Esc-menu setting (1 = default).
        var scale = lookScale * LocalPlayerProfile.Sensitivity;
        float mouseX = look.x * lookSensitivity * (1 + mouseSensitivity.x) * scale;
        float mouseY = look.y * lookSensitivity * (1 + mouseSensitivity.y) * scale;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80, 80f);
        ApplyPitch();

        characterVisualisationRoot.Rotate(Vector3.up * mouseX);
    }

    private void ApplyPitch() {
        head.localRotation = Quaternion.Euler(Mathf.Clamp(xRotation - kickOffset, -85f, 85f), 0f, 0f);
    }

}
