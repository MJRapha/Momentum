// PlayerMovementController.cs
//
// Core movement for "Momentum". Handles running, jumping, double jumping,
// wall running, wall jumping and grapple pulls. Built on Unity's
// CharacterController (no Rigidbody needed).
//
// This script is the ONLY place that calls controller.Move(). Other
// movement sources (e.g. GrappleHookController) feed velocity into it, so
// speed from any source is preserved and shows up in CurrentSpeed/Velocity.
//
// SETUP:
//   1. Create an empty GameObject named "Player".
//   2. Add a CharacterController component to it.
//   3. Add this script to the same GameObject.
//   4. Child the main Camera under "Player" and assign it to "playerCamera".
//   5. Create two Layers named "Ground" and "Wall", assign them to your
//      level geometry, and set them in the Inspector fields below.
//      (Momentum > Build Gray-Box Test Level does all of this for you.)
//
// INPUT (default Unity Input Manager):
//   Horizontal / Vertical -> WASD, Jump -> Space, Left Shift -> sprint

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Header("Ground Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float sprintMultiplier = 1.4f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float airControlMultiplier = 0.5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float doubleJumpForce = 7f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Run")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.7f;
    [SerializeField] private float wallRunSpeed = 9f;          // minimum speed; faster entries keep their speed
    [SerializeField] private float wallRunGravity = -4f;
    [SerializeField] private float wallRunMaxDuration = 2.5f;

    [Header("Wall Jump (jump to the other side, like MotorSlice)")]
    [SerializeField] private float wallJumpUpForce = 8.5f;
    [SerializeField] private float wallJumpSideForce = 8f;     // push away from the wall
    [SerializeField] private float wallJumpForwardForce = 4f;  // keeps you moving forward
    [SerializeField] private float wallJumpLockTime = 0.25f;   // no re-grab / air-steering right after the jump

    private CharacterController controller;
    private GrappleHookController grapple;
    private Vector3 velocity;
    private Vector3 currentMoveDirection;

    private bool isGrounded;
    private bool hasDoubleJumped;

    private bool isWallRunning;
    private float wallRunTimer;
    private float wallRunEntrySpeed;
    private Vector3 wallNormal;
    private Collider currentWallCollider;
    private Collider lastWallCollider; // the wall we just left; can't be re-grabbed until we land
    private float wallJumpLockTimer;

    // Exposed so other scripts (e.g. the momentum attack) can read current speed.
    /// <summary>Horizontal speed (m/s), from any movement source including the grapple.</summary>
    public float CurrentSpeed { get; private set; }
    /// <summary>Full 3D velocity the player moved with this frame.</summary>
    public Vector3 Velocity => velocity;
    public bool IsWallRunning => isWallRunning;
    public bool IsGrounded => isGrounded;

    private Vector3 BodyCenter => transform.position + controller.center;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        grapple = GetComponent<GrappleHookController>();
    }

    private void Update()
    {
        if (wallJumpLockTimer > 0f)
        {
            wallJumpLockTimer -= Time.deltaTime;
        }

        CheckGround();

        if (grapple != null && grapple.IsGrappling)
        {
            HandleGrappleMovement();
        }
        else
        {
            CheckWallRun();

            if (isWallRunning)
            {
                HandleWallRunMovement();
            }
            else if (wallJumpLockTimer <= 0f)
            {
                HandleGroundAndAirMovement();
            }
            // While the wall-jump lock is active we keep the launch velocity untouched.

            HandleJumpInput();
            ApplyGravity();
        }

        controller.Move(velocity * Time.deltaTime);

        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        CurrentSpeed = horizontalVelocity.magnitude;
    }

    private void CheckGround()
    {
        // Short sphere cast from just above the feet. The old ray was
        // height/2 + distance long from the feet, which counted the player as
        // grounded up to ~1 m in the air.
        float radius = controller.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        bool hitGround = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.05f,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        // Moving upward (just jumped / launched) never counts as grounded.
        isGrounded = hitGround && velocity.y <= 0.1f;

        if (isGrounded)
        {
            hasDoubleJumped = false;
            lastWallCollider = null; // landing makes every wall grabbable again
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }
        }
    }

    private void HandleGroundAndAirMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = (transform.right * horizontalInput + transform.forward * verticalInput);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        float targetSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 targetVelocity = inputDirection * targetSpeed;

        float lerpFactor = acceleration * Time.deltaTime * (isGrounded ? 1f : airControlMultiplier);

        float currentSpeed = currentMoveDirection.magnitude;
        if (!isGrounded && currentSpeed > targetSpeed)
        {
            // Momentum preservation: in the air, faster-than-run speed (from a
            // grapple slingshot or wall jump) is kept. Input only steers it.
            if (inputDirection.sqrMagnitude > 0.01f)
            {
                Vector3 steered = Vector3.Lerp(currentMoveDirection, inputDirection.normalized * currentSpeed, lerpFactor);
                currentMoveDirection = steered.normalized * currentSpeed;
            }
        }
        else
        {
            // Grounded (acts as friction) or below run speed: normal acceleration.
            currentMoveDirection = Vector3.Lerp(currentMoveDirection, targetVelocity, lerpFactor);
        }

        velocity.x = currentMoveDirection.x;
        velocity.z = currentMoveDirection.z;
    }

    private void HandleJumpInput()
    {
        if (!Input.GetButtonDown("Jump"))
        {
            return;
        }

        if (isWallRunning)
        {
            WallJump();
            return;
        }

        if (isGrounded)
        {
            velocity.y = jumpForce;
        }
        else if (!hasDoubleJumped)
        {
            velocity.y = doubleJumpForce;
            hasDoubleJumped = true;
        }
    }

    private void ApplyGravity()
    {
        float currentGravity = isWallRunning ? wallRunGravity : gravity;
        velocity.y += currentGravity * Time.deltaTime;
    }

    // ---------------- Grapple ----------------

    private void HandleGrappleMovement()
    {
        EndWallRun();

        // Pull velocity replaces our own. Gravity is skipped so it can't build
        // up during the pull. When the grapple releases, 'velocity' still holds
        // the pull vector -> the player is slingshotted forward.
        velocity = grapple.GetPullVelocity(BodyCenter);
        currentMoveDirection = new Vector3(velocity.x, 0f, velocity.z);

        if (Input.GetButtonDown("Jump"))
        {
            // Jump out of the grapple, keeping horizontal momentum.
            grapple.StopGrapple();
            velocity.y = Mathf.Max(velocity.y, jumpForce);
            hasDoubleJumped = false;
        }
    }

    // ---------------- Wall Run ----------------

    private void CheckWallRun()
    {
        if (isGrounded)
        {
            EndWallRun();
            return;
        }

        if (wallJumpLockTimer > 0f)
        {
            return; // just wall-jumped: don't grab a wall again yet
        }

        bool hasForwardInput = Input.GetAxisRaw("Vertical") > 0.1f;

        RaycastHit hit;
        bool foundWall = false;
        Vector3 origin = BodyCenter;

        if (Physics.Raycast(origin, transform.right, out hit, wallCheckDistance, wallLayer)
            && hit.collider != lastWallCollider)
        {
            foundWall = true;
        }
        else if (Physics.Raycast(origin, -transform.right, out hit, wallCheckDistance, wallLayer)
                 && hit.collider != lastWallCollider)
        {
            foundWall = true;
        }

        if (foundWall && hasForwardInput)
        {
            if (!isWallRunning)
            {
                StartWallRun();
            }

            currentWallCollider = hit.collider;
            wallNormal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;

            wallRunTimer += Time.deltaTime;
            if (wallRunTimer >= wallRunMaxDuration)
            {
                lastWallCollider = currentWallCollider; // fall off, can't re-stick to the same wall
                EndWallRun();
            }
        }
        else if (isWallRunning)
        {
            EndWallRun();
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        wallRunEntrySpeed = Mathf.Max(wallRunSpeed, CurrentSpeed); // keep incoming momentum
        hasDoubleJumped = false; // grabbing a wall refreshes the double jump
        if (velocity.y < 0f)
        {
            velocity.y = 0f; // "catch" the wall instead of sliding down fast
        }
    }

    private void EndWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
    }

    private void HandleWallRunMovement()
    {
        // Run along the wall surface rather than into it.
        Vector3 wallForward = Vector3.ProjectOnPlane(transform.forward, wallNormal);
        wallForward.y = 0f;
        if (wallForward.sqrMagnitude < 0.0001f)
        {
            wallForward = transform.forward;
        }
        wallForward.Normalize();

        velocity.x = wallForward.x * wallRunEntrySpeed;
        velocity.z = wallForward.z * wallRunEntrySpeed;
        currentMoveDirection = new Vector3(velocity.x, 0f, velocity.z);
    }

    private void WallJump()
    {
        // Push away from the wall (toward the other side) while keeping forward momentum.
        Vector3 push = wallNormal * wallJumpSideForce + transform.forward * wallJumpForwardForce;
        velocity = new Vector3(push.x, wallJumpUpForce, push.z);
        currentMoveDirection = new Vector3(velocity.x, 0f, velocity.z);

        lastWallCollider = currentWallCollider; // this wall can't be re-grabbed until we land
        wallJumpLockTimer = wallJumpLockTime;
        EndWallRun();
    }
}
