using System.Collections.Generic;
using UnityEngine;

// Ported from 3dPixelZombies (fpsTesting branch).
// Changes from the original: input comes from PlayerInputReader (new Input System),
// HumanoidAnimator and FootstepsController hooks were removed, coyote time counts down with
// Time.deltaTime (the original used fixedDeltaTime in Update, which made it frame-rate dependent),
// ground checks ignore trigger colliders so players can't stand on ladders or kill zones.
public class PlayerMovementController : MonoBehaviour, IMovementController
{

    [SerializeField]
    private Rigidbody body;

    [SerializeField]
    private Transform playerVisualisation;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private float movementSpeed = 90f;

    [SerializeField]
    private float sprintingSpeedBonus = 40f;

    // Jump apex above the float height, in metres. The launch speed is worked out from it.
    [SerializeField, Min(0f)]
    private float jumpHeight = 1f;

    // Gravity multipliers while airborne under normal movement (snappier arcs). Off while the float
    // spring holds the body, and whenever this controller is disabled (climbing, swinging, launches).
    [SerializeField, Min(1f)]
    private float riseGravityScale = 1.5f;

    [SerializeField, Min(1f)]
    private float fallGravityScale = 2f;

    [SerializeField]
    private float coyoteTime = 0.1f;

    // A jump pressed this long before landing still happens on landing.
    [SerializeField, Min(0f)]
    private float jumpBufferTime = 0.12f;

    [SerializeField]
    private float climbingSpeed = 100f;

    [Header("FLOATING")]
    [Space(20)]
    [SerializeField]
    private float rayLength = 0.61f;
    [SerializeField]
    private float groundCheckRayLength = 0.52f;
    [SerializeField]
    private float sphereRadius = .06f;
    [SerializeField]
    private float rideSpringStrength = 1000f;
    [SerializeField]
    private float rideSpringHeight = .45f;
    [SerializeField]
    private float rideSpringDampening = 36f;
    [SerializeField]
    private float maxSlopeAngle = 35f;
    [SerializeField]
    private float slideDownSpeed = 25f;
    [SerializeField]
    [Range(0, 0.7f)]
    private float stoppingPower = 0.7f;

    [SerializeField]
    private LayerMask walkableLayers = ~0;


    // Each physics step the controller cancels this share of the horizontal velocity and adds the
    // target step; together they settle at speed * fixedDeltaTime / BrakeFactor (see GroundSpeed).
    private const float BrakeFactor = 0.7f;

    private float jumpCD = 0.25f;
    private float currentJumpCD = 0f;
    private float currentCoyoteTime = 0f;
    private bool canJump = false;
    private float lastJumpPressTime = float.NegativeInfinity;

    // v = sqrt(2gh), plus half a physics step of gravity: the stepped simulation would otherwise
    // fall short of the apex by v * fixedDeltaTime / 2 (~5 cm).
    private float JumpSpeed
    {
        get
        {
            var gravity = -Physics.gravity.y * riseGravityScale;
            return Mathf.Sqrt(2f * gravity * jumpHeight) + 0.5f * gravity * Time.fixedDeltaTime;
        }
    }

    private bool grounded = false;
    private bool wasGrounded = true;
    private float airborneTime;
    private float airborneMinVelocityY;

    private Vector3 movementVector;

    private float bonusMovementSpeed;
    public Vector3 MovementDirection => movementVector;

    private bool locked;

    // Set by status effects (tranq sleep, net), separate from the countdown/menu lock.
    private bool blocked;

    // Speed scale from effects (tranq slow, carrying). 1 = normal. Product of all sources.
    private float speedMultiplier = 1f;
    private readonly Dictionary<object, float> speedSources = new();

    private bool isClimbing;

    // Head-ride tug of war: a world-space push from the rider (flat, length 0-1) mixed with this
    // player's own input at ownInputWeight. No sprinting while steered.
    private bool steered;
    private Vector3 steerPush;
    private float steerOwnWeight = 1f;

    public bool IsGrounded => grounded;

    // Horizontal speed of the body in m/s.
    public float FlatSpeed
    {
        get
        {
            var velocity = body.linearVelocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }
    }

    // Sprint held while moving under the player's own control (not steered by a rider).
    public bool SprintRequested => enabled && !locked && !blocked && !steered && !isClimbing
        && input.SprintHeld && input.Move.sqrMagnitude > 0.01f;

    public event System.Action OnJumped;

    // Downward speed (m/s) at touchdown, after at least a short time in the air.
    public event System.Action<float> OnLanded;

    public void SetSteer(Vector3 worldPush, float ownInputWeight)
    {
        steered = true;
        worldPush.y = 0f;
        steerPush = Vector3.ClampMagnitude(worldPush, 1f);
        steerOwnWeight = Mathf.Clamp01(ownInputWeight);
    }

    public void ClearSteer()
    {
        steered = false;
        steerPush = Vector3.zero;
        steerOwnWeight = 1f;
    }

    public void UpdateBonusMovementSpeed(float rawMoveSpeedBonus)
    {
        bonusMovementSpeed = rawMoveSpeedBonus;
    }
    public void LockMovement(bool locked)
    {
        this.locked = locked;
    }

    public void SetBlocked(bool blocked)
    {
        this.blocked = blocked;
    }

    // Actual steady ground speed in m/s for walking or sprinting (before status-effect slow).
    // Climbing uses it so climbing matches walking / sprinting.
    public float GroundSpeed(bool sprinting)
    {
        var speed = movementSpeed + (sprinting ? sprintingSpeedBonus : 0f) + bonusMovementSpeed;
        return speed * Time.fixedDeltaTime / BrakeFactor;
    }

    // Effect speed scale (tranq slow, carrying); climbing and swinging use it too.
    public float SpeedMultiplier => speedMultiplier;

    // Each effect sets its own multiplier; 1 removes it. The effective scale is their product.
    public void SetSpeedMultiplier(object source, float multiplier)
    {
        if (Mathf.Approximately(multiplier, 1f))
        {
            speedSources.Remove(source);
        } else
        {
            speedSources[source] = Mathf.Max(0f, multiplier);
        }
        speedMultiplier = 1f;
        foreach (var value in speedSources.Values)
        {
            speedMultiplier *= value;
        }
    }

    public void ToggleClimbing(bool isClimbing)
    {
        this.isClimbing = isClimbing;
    }
    private void OnEnable()
    {
        // A press from before climbing / swinging / a launch must not fire later.
        lastJumpPressTime = float.NegativeInfinity;
    }

    private void Update()
    {

        grounded = Physics.Raycast(this.transform.position, Vector3.down, groundCheckRayLength, walkableLayers, QueryTriggerInteraction.Ignore);
        TrackLanding();
        // Counts down even while locked or blocked: the float spring stays off until it runs out.
        currentJumpCD -= Time.deltaTime;

        if (locked || blocked)
        {
            movementVector = Vector3.zero;
            return;
        }
        var move = input.Move;
        movementVector = new Vector3(move.y, 0, move.x);

        if (movementVector.magnitude > 0)
        {
            if (input.SprintHeld)
            {
                movementVector.y = sprintingSpeedBonus;
            }
        }
        HandleJump();
    }
    private void FixedUpdate()
    {
        if (!isClimbing)
        {
            HandleFloating();
        } else
        {
            //stop the player from sliding down randomly
            //check if the player is grounded!
            if (movementVector.x == 0)
            {
                body.AddForce(-Physics.gravity, ForceMode.Acceleration);
            }

            var upwards = Vector3.up * movementVector.x;

            if (movementVector.x < 0)
            {
                var floatingRay = Physics.SphereCast(this.transform.position, sphereRadius, Vector3.down, out var hit, rayLength, walkableLayers, QueryTriggerInteraction.Ignore);

                if (floatingRay)
                {
                    upwards = playerVisualisation.forward * movementVector.x;

                }
            }
            var sideways = playerVisualisation.right * movementVector.z;
            var newMoveVector = (upwards + sideways) * (climbingSpeed * speedMultiplier) * Time.fixedDeltaTime;
            body.linearVelocity = newMoveVector;


        }
    }

    public void ToggleRigidBody(bool isEnabled)
    {
        body.isKinematic = isEnabled;
    }



    private void HandleJump()
    {
        if (grounded)
        {
            currentCoyoteTime = coyoteTime;
        } else
        {
            currentCoyoteTime -= Time.deltaTime;
        }
        if (input.JumpPressed)
        {
            lastJumpPressTime = Time.time;
        }
        var jumpQueued = Time.time - lastJumpPressTime <= jumpBufferTime;
        if (jumpQueued && currentCoyoteTime >= 0 && canJump)
        {
            var velocity = body.linearVelocity;
            velocity.y = JumpSpeed;
            body.linearVelocity = velocity;
            canJump = false;
            currentJumpCD = jumpCD;
            lastJumpPressTime = float.NegativeInfinity;
            OnJumped?.Invoke();
        }

        //Maybe change in case of double jump?
        if (currentJumpCD <= 0 && grounded)
        {
            canJump = true;
        }
    }


    // Short dips between stair steps don't count as a landing.
    private void TrackLanding()
    {
        if (!grounded)
        {
            airborneTime += Time.deltaTime;
            airborneMinVelocityY = Mathf.Min(airborneMinVelocityY, body.linearVelocity.y);
        } else if (!wasGrounded && airborneTime > 0.15f)
        {
            OnLanded?.Invoke(-airborneMinVelocityY);
        }
        if (grounded)
        {
            airborneTime = 0f;
            airborneMinVelocityY = 0f;
        }
        wasGrounded = grounded;
    }

    private void HandleFloating()
    {
        var floatingRay = Physics.SphereCast(this.transform.position, sphereRadius, Vector3.down, out var hit, rayLength, walkableLayers, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(this.transform.position, Vector3.down * rayLength, Color.red);
        Debug.DrawRay(this.transform.position, Vector3.down * rideSpringHeight, Color.green);

        var rayDir = Vector3.down;
        var force = 0f;
        bool slopeTooSteep = false;

        if (floatingRay && currentJumpCD <= 0)
        {
            // Check slope angle
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            slopeTooSteep = slopeAngle > maxSlopeAngle;

            var vel = body.linearVelocity;
            rayDir = transform.TransformDirection(Vector3.down);
            var rayDirVel = Vector3.Dot(rayDir, vel);
            var otherVel = Vector3.zero;
            var hitbody = hit.rigidbody;
            if (hitbody != null)
            {
                otherVel = hitbody.linearVelocity;
            }
            var otherDirVel = Vector3.Dot(rayDir, otherVel);
            var relVel = rayDirVel - otherDirVel;
            var x = hit.distance - rideSpringHeight;
            force = (x * rideSpringStrength) - (relVel * rideSpringDampening);
        }

        var forward = playerVisualisation.forward * movementVector.x;
        var sideways = playerVisualisation.right * movementVector.z;
        var moveDirection = forward + sideways;
        var speed = movementSpeed + movementVector.y + bonusMovementSpeed;
        // Steering ignores menus (the rider keeps pushing) but not status effects that hold the body.
        if (steered && !blocked)
        {
            moveDirection = Vector3.ClampMagnitude(moveDirection * steerOwnWeight + steerPush, 1f);
            speed = movementSpeed + bonusMovementSpeed;
        }
        var newMoveVector = moveDirection * (speed * speedMultiplier);

        if (floatingRay)
        {
            if (slopeTooSteep)
            {
                // Project movement onto slope so you can still move sideways, not into the slope
                newMoveVector = Vector3.ProjectOnPlane(newMoveVector, hit.normal);

                // Add sliding down vector
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                newMoveVector += slideDir * slideDownSpeed;
            } else
            {
                // For walkable slopes, project to avoid floating off the ground
                newMoveVector = Vector3.ProjectOnPlane(newMoveVector, hit.normal);
            }
        }

        var clamped = Vector3.ClampMagnitude(newMoveVector, speed * speedMultiplier) * Time.fixedDeltaTime;
        var tempVelocity = Vector3.zero;
        tempVelocity.x = clamped.x;
        tempVelocity.z = clamped.z;
        tempVelocity.y = 0;
        var bodyVelocity = body.linearVelocity;
        bodyVelocity.y = 0;
        if (newMoveVector == Vector3.zero && grounded)
        {
            bodyVelocity.Scale(new Vector3(stoppingPower, 0, stoppingPower));
        }
        var newVelocity = (tempVelocity - (bodyVelocity * BrakeFactor)) / Time.fixedDeltaTime;
        var rayForce = rayDir * force;
        var desiredMoveDir = newVelocity + rayForce;

        // Heavier gravity in the air; the spring alone handles the body near the ground.
        var springHolding = floatingRay && currentJumpCD <= 0;
        if (!springHolding && body.useGravity)
        {
            var gravityScale = body.linearVelocity.y > 0f ? riseGravityScale : fallGravityScale;
            desiredMoveDir += Physics.gravity * (gravityScale - 1f);
        }

        body.AddForce(desiredMoveDir, ForceMode.Acceleration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(this.transform.position, sphereRadius);
        Gizmos.DrawWireSphere(this.transform.position + Vector3.down * rayLength, sphereRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(this.transform.position + Vector3.down * groundCheckRayLength, 0.01f);

    }
}
