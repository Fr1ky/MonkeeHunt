using PurrNet;
using UnityEngine;

// Banana-peel trip. The server tells the owner to fall; the owner tips the Visualisation (body, head,
// camera) backwards in a short animation, lies there with move / look / actions blocked, then stands
// back up. Others see it through Visualisation's existing rotation sync; the root and its collider
// stay upright (approved trade-off).
public class TripStatus : NetworkBehaviour {

    private enum Phase {
        None,
        Falling,
        Lying,
        Rising
    }

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private Transform visualisation;

    [SerializeField, Min(0.1f)]
    private float downSeconds = 3f;

    [SerializeField, Min(0.05f)]
    private float fallSeconds = 0.3f;

    [SerializeField, Min(0.05f)]
    private float riseSeconds = 0.4f;

    // Negative = falls backwards (feet slip forward).
    [SerializeField]
    private float tipAngle = -85f;

    private Phase phase;
    private float phaseStart;
    private Quaternion upright;
    private Quaternion fallen;

    // Server: when the last trip (or head-ride throw) ends, so the server can tell a body is down.
    private float trippedUntilServer;

    public bool IsTripped => phase != Phase.None;

    // Server: down from a trip or a head-ride throw right now.
    public bool IsTrippedServer => isServer && Time.time < trippedUntilServer;

    // Server.
    public void TripServer() {
        if (isServer && owner.HasValue) {
            // A tripping carrier drops the monkey it carries (still netted).
            if (TryGetComponent(out HunterCarry carry)) {
                carry.DropServer();
            }
            NoteDownServer();
            Trip(owner.Value);
        }
    }

    // Server: a head-rider was thrown off; its owner plays the fall itself (BeginLocal).
    public void NoteDownServer() {
        if (isServer) {
            trippedUntilServer = Time.time + downSeconds;
        }
    }

    [TargetRpc]
    private void Trip(PlayerID target) {
        BeginLocal();
    }

    // Owner: tip over, lie down, stand back up (downSeconds in total).
    public void BeginLocal() {
        if (phase != Phase.None) {
            return;
        }
        // Keep the current facing (yaw) and tip around the body's own sideways axis.
        upright = Quaternion.Euler(0f, visualisation.localEulerAngles.y, 0f);
        fallen = upright * Quaternion.Euler(tipAngle, 0f, 0f);
        SetPhase(Phase.Falling);
        player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.Movement
            | PlayerNetworkController.StatusBlock.Look | PlayerNetworkController.StatusBlock.Actions);
    }

    private void Update() {
        if (phase == Phase.None) {
            return;
        }
        var t = Time.time - phaseStart;
        switch (phase) {
            case Phase.Falling:
                visualisation.localRotation = Quaternion.Slerp(upright, fallen, EaseIn(t / fallSeconds));
                if (t >= fallSeconds) {
                    SetPhase(Phase.Lying);
                }
                break;
            case Phase.Lying:
                visualisation.localRotation = fallen;
                if (t >= downSeconds - fallSeconds - riseSeconds) {
                    SetPhase(Phase.Rising);
                }
                break;
            case Phase.Rising:
                visualisation.localRotation = Quaternion.Slerp(fallen, upright, Mathf.SmoothStep(0f, 1f, t / riseSeconds));
                if (t >= riseSeconds) {
                    Stand();
                }
                break;
        }
    }

    private void Stand() {
        visualisation.localRotation = upright;
        phase = Phase.None;
        player.SetStatusBlock(this, PlayerNetworkController.StatusBlock.None);
    }

    private void SetPhase(Phase next) {
        phase = next;
        phaseStart = Time.time;
    }

    private static float EaseIn(float x) {
        x = Mathf.Clamp01(x);
        return x * x;
    }

    private void OnDisable() {
        if (phase != Phase.None && player) {
            Stand();
        }
    }
}
