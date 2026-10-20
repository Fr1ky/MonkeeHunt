using UnityEngine;

// Monkey only, owner only. Jump while touching a Climbable surface to grab on; the normal movement
// controller is switched off while climbing. W/S climb up/down, A/D sideways (at walking speed,
// Shift sprints - same speeds as on the ground), Jump leaps off
// (away from the surface + up). Climbing past the top or off the side drops back to normal movement.
public class MonkeyClimb : MonoBehaviour {

    private const int MaxOverlaps = 16;

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private CapsuleCollider bodyCollider;

    [SerializeField]
    private Transform visualisation;

    [SerializeField]
    private MonkeySwing swing;

    // Climb speed as a share of the monkey's ground speed (1 = same as walking / sprinting).
    [SerializeField, Min(0.1f)]
    private float climbSpeedScale = 1f;

    [SerializeField]
    private float leapAwaySpeed = 6f;

    [SerializeField]
    private float leapUpSpeed = 4f;

    // Extra reach around the body capsule when looking for a climbable surface.
    [SerializeField]
    private float reach = 0.25f;

    // Small push towards the surface so the body stays on it.
    [SerializeField]
    private float stickSpeed = 0.5f;

    // Boost when climbing over the top, so the monkey lands on top instead of sliding back down.
    [SerializeField]
    private float topOutUpSpeed = 3f;

    [SerializeField]
    private float topOutForwardSpeed = 2f;

    // The normal controller brakes horizontal speed hard even in the air, so after a leap / top-out
    // it stays off this long to let the jump actually carry the monkey.
    [SerializeField]
    private float airControlDelay = 0.35f;

    [SerializeField]
    private LayerMask surfaceLayers = ~0;

    private readonly Collider[] overlaps = new Collider[MaxOverlaps];
    private Climbable current;
    private Vector3 surfaceNormal;
    private Vector2 moveInput;
    private bool sprinting;
    private float reenableMovementTime = -1f;

    public bool IsClimbing => current;

    // A climbable surface is within reach right now (Jump would grab it).
    public bool IsTouchingClimbable => TryFindSurface(out _, out _);

    private void Update() {
        if (!player.isOwner) {
            return;
        }
        if (reenableMovementTime > 0f && Time.time >= reenableMovementTime) {
            reenableMovementTime = -1f;
            if (!IsClimbing && (!swing || !swing.IsSwinging)) {
                movement.enabled = true;
            }
        }
        if (IsClimbing) {
            if (player.ControlsLocked) {
                moveInput = Vector2.zero;
                sprinting = false;
                return;
            }
            moveInput = input.Move;
            sprinting = input.SprintHeld;
            if (input.JumpPressed) {
                LeapOff();
            }
            return;
        }
        if (input.JumpPressed && !player.ActionsBlocked && !player.MovementLocked && !player.StatusBlocksMovement
            && TryFindSurface(out var climbable, out var normal)) {
            StartClimb(climbable, normal);
        }
    }

    private void FixedUpdate() {
        if (!IsClimbing) {
            return;
        }
        if (player.StatusBlocksMovement) {
            StopClimb();
            return;
        }
        if (!TryFindSurface(out var climbable, out var normal)) {
            // Past the top (or off the side): hop up and over instead of sliding back down.
            var wasNormal = surfaceNormal;
            var climbingUp = moveInput.y > 0.1f;
            StopClimb();
            if (climbingUp) {
                body.linearVelocity = Vector3.up * topOutUpSpeed - wasNormal * topOutForwardSpeed;
                DelayMovement();
            }
            return;
        }
        current = climbable;
        surfaceNormal = normal;

        var up = Vector3.ProjectOnPlane(Vector3.up, surfaceNormal).normalized;
        var right = Vector3.Cross(up, surfaceNormal).normalized;
        // Sideways relative to where the player looks: A/D should move left/right on screen.
        if (Vector3.Dot(right, visualisation.right) < 0f) {
            right = -right;
        }
        var speed = movement.GroundSpeed(sprinting) * climbSpeedScale * current.climbSpeedMultiplier
            * movement.SpeedMultiplier;
        body.linearVelocity = (up * moveInput.y + right * moveInput.x) * speed - surfaceNormal * stickSpeed;
    }

    private void StartClimb(Climbable climbable, Vector3 normal) {
        reenableMovementTime = -1f;
        current = climbable;
        surfaceNormal = normal;
        movement.enabled = false;
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
    }

    private void StopClimb() {
        current = null;
        moveInput = Vector2.zero;
        body.useGravity = true;
        if (player.isOwner) {
            movement.enabled = true;
        }
    }

    private void LeapOff() {
        var away = surfaceNormal;
        StopClimb();
        body.linearVelocity = away * leapAwaySpeed + Vector3.up * leapUpSpeed;
        DelayMovement();
    }

    private void DelayMovement() {
        movement.enabled = false;
        reenableMovementTime = Time.time + airControlDelay;
    }

    // Nearest climbable collider around the body; normal points from the surface to the body
    // (horizontal, so tree trunks and rock faces both work).
    private bool TryFindSurface(out Climbable climbable, out Vector3 normal) {
        climbable = null;
        normal = Vector3.zero;
        var centre = transform.TransformPoint(bodyCollider.center);
        var half = Mathf.Max(0f, bodyCollider.height * 0.5f - bodyCollider.radius);
        var a = centre + Vector3.up * half;
        var b = centre - Vector3.up * half;
        var count = Physics.OverlapCapsuleNonAlloc(a, b, bodyCollider.radius + reach, overlaps, surfaceLayers,
            QueryTriggerInteraction.Ignore);
        var best = float.MaxValue;
        for (int i = 0; i < count; i++) {
            var col = overlaps[i];
            if (col.transform.IsChildOf(transform)) {
                continue;
            }
            var c = col.GetComponentInParent<Climbable>();
            if (!c) {
                continue;
            }
            var closest = col.ClosestPoint(centre);
            var toBody = centre - closest;
            toBody.y = 0f;
            var dist = toBody.magnitude;
            if (dist < 0.01f || dist >= best) {
                continue;
            }
            best = dist;
            climbable = c;
            normal = toBody / dist;
        }
        return climbable;
    }

    private void OnDisable() {
        if (IsClimbing) {
            StopClimb();
        }
        if (reenableMovementTime > 0f) {
            reenableMovementTime = -1f;
            if (player && player.isOwner) {
                movement.enabled = true;
            }
        }
    }
}
