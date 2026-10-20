using PurrNet;
using UnityEngine;

// Monkey only. Jump within grab range of a SwingPoint (nearest one in front wins) to hang from it on a
// rope of the current length. Gravity + a taut-rope constraint make the pendulum; W/S pump the swing
// along the look direction; Jump lets go with the swing's momentum plus a small upward boost.
// The owner simulates; the anchor and "attached" are synced so everyone sees the rope.
public class MonkeySwing : NetworkBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private MonkeyClimb climb;

    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private Transform visualisation;

    // Where the rope attaches on the body (local).
    [SerializeField]
    private Vector3 gripOffset = new Vector3(0f, 0.6f, 0f);

    [SerializeField]
    private Transform ropeVisual;

    [SerializeField]
    private float grabRange = 3f;

    [SerializeField]
    private float pumpAcceleration = 12f;

    [SerializeField]
    private float releaseUpBoost = 2f;

    [SerializeField]
    private float minRopeLength = 0.75f;

    // Same reason as MonkeyClimb: the normal controller brakes hard in the air, so it stays off
    // briefly after letting go to keep the swing's momentum.
    [SerializeField]
    private float airControlDelay = 0.6f;

    [SerializeField]
    private SyncVar<bool> attached = new(false, 0f, true);

    [SerializeField]
    private SyncVar<Vector3> anchor = new(Vector3.zero, 0f, true);

    private float ropeLength;
    private float pumpInput;
    private float reenableMovementTime = -1f;

    public bool IsSwinging => attached.value;

    private Vector3 Grip => transform.TransformPoint(gripOffset);

    private void Update() {
        UpdateRopeVisual();
        if (!isOwner) {
            return;
        }
        if (reenableMovementTime > 0f && Time.time >= reenableMovementTime) {
            reenableMovementTime = -1f;
            if (!IsSwinging && !climb.IsClimbing) {
                movement.enabled = true;
            }
        }
        if (IsSwinging) {
            pumpInput = player.ControlsLocked ? 0f : input.Move.y;
            if (input.JumpPressed && !player.ControlsLocked) {
                Release();
            }
            return;
        }
        // Climbing takes priority when a climbable surface is in reach.
        if (input.JumpPressed && !player.ActionsBlocked && !player.MovementLocked && !player.StatusBlocksMovement
            && !climb.IsClimbing
            && !climb.IsTouchingClimbable
            && TryFindSwingPoint(out var point)) {
            Grab(point);
        }
    }

    private void FixedUpdate() {
        if (!isOwner || !IsSwinging) {
            return;
        }
        if (player.StatusBlocksMovement) {
            Release();
            return;
        }

        // Pump along the horizontal look direction.
        var forward = Vector3.ProjectOnPlane(visualisation.forward, Vector3.up).normalized;
        body.AddForce(forward * (pumpInput * pumpAcceleration * movement.SpeedMultiplier), ForceMode.Acceleration);

        // Taut rope: keep the grip on the sphere around the anchor and drop outward speed.
        var grip = Grip;
        var fromAnchor = grip - anchor.value;
        var distance = fromAnchor.magnitude;
        if (distance > ropeLength && distance > 0.001f) {
            var dir = fromAnchor / distance;
            body.position += -dir * (distance - ropeLength);
            var velocity = body.linearVelocity;
            var outward = Vector3.Dot(velocity, dir);
            if (outward > 0f) {
                body.linearVelocity = velocity - dir * outward;
            }
        }
    }

    private bool TryFindSwingPoint(out SwingPoint best) {
        best = null;
        var grip = Grip;
        var forward = Vector3.ProjectOnPlane(visualisation.forward, Vector3.up).normalized;
        var bestDistance = float.MaxValue;
        foreach (var point in SwingPoint.All) {
            var offset = point.Anchor - grip;
            var distance = offset.magnitude;
            if (distance > grabRange || distance >= bestDistance) {
                continue;
            }
            // "In front": not behind the player (directly overhead counts).
            var flat = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (flat.sqrMagnitude > 0.04f && Vector3.Dot(flat.normalized, forward) < 0f) {
                continue;
            }
            bestDistance = distance;
            best = point;
        }
        return best;
    }

    private void Grab(SwingPoint point) {
        reenableMovementTime = -1f;
        anchor.value = point.Anchor;
        ropeLength = Mathf.Max(minRopeLength, Vector3.Distance(Grip, point.Anchor));
        attached.value = true;
        movement.enabled = false;
        body.useGravity = true;
    }

    private void Release() {
        attached.value = false;
        pumpInput = 0f;
        // Something may already hold the body still (e.g. trapped in quicksand).
        if (!body.isKinematic) {
            body.linearVelocity += Vector3.up * releaseUpBoost;
        }
        reenableMovementTime = Time.time + airControlDelay;
    }

    private void UpdateRopeVisual() {
        if (!ropeVisual) {
            return;
        }
        ropeVisual.gameObject.SetActive(attached.value);
        if (!attached.value) {
            return;
        }
        var from = Grip;
        var to = anchor.value;
        var length = Vector3.Distance(from, to);
        ropeVisual.position = (from + to) * 0.5f;
        ropeVisual.up = (to - from).normalized;
        // Unity cylinder is 2 units tall.
        var scale = ropeVisual.localScale;
        ropeVisual.localScale = new Vector3(scale.x, length * 0.5f, scale.z);
    }

    private void OnDisable() {
        if (isOwner && IsSwinging) {
            attached.value = false;
        }
        if (reenableMovementTime > 0f) {
            reenableMovementTime = -1f;
            if (player && player.isOwner && climb && !climb.IsClimbing) {
                movement.enabled = true;
            }
        }
    }
}
