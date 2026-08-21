using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public Transform visualRoot;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 4.0f;
    public float runSpeed = 7.0f;
    public float acceleration = 30.0f;
    public float deceleration = 35.0f;
    public float rotationSpeed = 14.0f;

    [Header("Jump")]
    public float jumpVelocity = 6.5f;
    public float groundCheckExtraDistance = 0.12f;
    public float jumpGroundIgnoreTime = 0.20f;
    public LayerMask groundMask = ~0;

    [Header("Dodge / Roll")]
    public float dodgeSpeed = 10.0f;
    public float dodgeDuration = 0.38f;
    public float dodgeCooldown = 0.65f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private PlayerAttack playerAttack;
    private Vector2 moveInput;
    private bool runHeld;
    private bool jumpRequested;
    private bool isGrounded;
    private float jumpGroundIgnoreTimer;
    private bool isDodging;
    private float dodgeTimer;
    private float dodgeCooldownTimer;
    private Vector3 dodgeDirection;

    public bool IsGrounded => isGrounded;
    public bool IsRunInput => runHeld && moveInput.sqrMagnitude > 0.01f && !isDodging;
    public Vector3 CurrentMoveDirection => GetCameraRelativeMoveDirection();

    private bool MovementLocked => playerAttack != null && playerAttack.IsMovementLocked;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerAttack = GetComponent<PlayerAttack>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        ResolveCamera();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (visualRoot == null && animator != null && animator.transform != transform) visualRoot = animator.transform;
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        ResolveCamera();
        ReadInput();
        if (dodgeCooldownTimer > 0f) dodgeCooldownTimer -= Time.deltaTime;
        if (jumpGroundIgnoreTimer > 0f) jumpGroundIgnoreTimer -= Time.deltaTime;

        if (Keyboard.current != null && !MovementLocked)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && !isDodging) jumpRequested = true;
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame && !isDodging && dodgeCooldownTimer <= 0f) StartDodge();
        }
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        if (MovementLocked)
        {
            if (playerAttack != null && playerAttack.IsDashAttacking && playerAttack.DashAttackElapsed < playerAttack.DashAttackMoveDuration)
                ApplyDashAttackMovement();
            else
                StopHorizontalMovement();
            return;
        }

        if (isDodging) ApplyDodge();
        else
        {
            ApplyMovement();
            ApplyJump();
        }
    }

    private void ResolveCamera()
    {
        if (cameraTransform != null) return;
        Camera mainCamera = Camera.main;
        if (mainCamera != null) cameraTransform = mainCamera.transform;
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            runHeld = false;
            return;
        }

        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;
        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        runHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }

    private void ApplyMovement()
    {
        Vector3 moveDirection = GetCameraRelativeMoveDirection();
        float targetSpeed = runHeld ? runSpeed : walkSpeed;
        Vector3 targetHorizontalVelocity = moveDirection * targetSpeed;
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float rate = moveDirection.sqrMagnitude > 0.001f ? acceleration : deceleration;
        Vector3 newHorizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetHorizontalVelocity, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newHorizontalVelocity.x, velocity.y, newHorizontalVelocity.z);

        if (visualRoot != null && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyDashAttackMovement()
    {
        Vector3 direction = playerAttack.DashDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(direction.x * playerAttack.DashAttackSpeed, velocity.y, direction.z * playerAttack.DashAttackSpeed);
        if (visualRoot != null) visualRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private Vector3 GetCameraRelativeMoveDirection()
    {
        if (moveInput.sqrMagnitude < 0.001f) return Vector3.zero;
        if (cameraTransform == null) return new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward = cameraForward.sqrMagnitude < 0.001f ? Vector3.forward : cameraForward.normalized;
        cameraRight = cameraRight.sqrMagnitude < 0.001f ? Vector3.right : cameraRight.normalized;
        return (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
    }

    private void ApplyJump()
    {
        if (!jumpRequested) return;
        if (isGrounded)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = jumpVelocity;
            rb.linearVelocity = velocity;
            isGrounded = false;
            jumpGroundIgnoreTimer = jumpGroundIgnoreTime;
            if (animator != null) animator.SetTrigger("doJump");
        }
        jumpRequested = false;
    }

    private void StartDodge()
    {
        Vector3 inputDirection = GetCameraRelativeMoveDirection();
        if (inputDirection.sqrMagnitude > 0.001f) dodgeDirection = inputDirection;
        else if (visualRoot != null)
        {
            dodgeDirection = visualRoot.forward;
            dodgeDirection.y = 0f;
            dodgeDirection.Normalize();
        }
        else
        {
            dodgeDirection = transform.forward;
            dodgeDirection.y = 0f;
            dodgeDirection.Normalize();
        }

        if (visualRoot != null && dodgeDirection.sqrMagnitude > 0.001f)
            visualRoot.rotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);

        isDodging = true;
        dodgeTimer = dodgeDuration;
        dodgeCooldownTimer = dodgeCooldown;
        jumpRequested = false;
        if (animator != null) animator.SetTrigger("doDodge");
    }

    private void ApplyDodge()
    {
        dodgeTimer -= Time.fixedDeltaTime;
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(dodgeDirection.x * dodgeSpeed, velocity.y, dodgeDirection.z * dodgeSpeed);
        if (dodgeTimer <= 0f) isDodging = false;
    }

    private void StopHorizontalMovement()
    {
        jumpRequested = false;
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    private void UpdateGrounded()
    {
        if (jumpGroundIgnoreTimer > 0f)
        {
            isGrounded = false;
            return;
        }
        if (rb.linearVelocity.y > 0.15f)
        {
            isGrounded = false;
            return;
        }

        Bounds bounds = capsule.bounds;
        float radius = Mathf.Max(0.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f);
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.03f, bounds.center.z);
        float distance = radius + groundCheckExtraDistance;
        isGrounded = Physics.SphereCast(origin, radius, Vector3.down, out _, distance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float normalizedSpeed = runSpeed > 0.01f ? Mathf.Clamp01(horizontalVelocity.magnitude / runSpeed) : 0f;
        animator.SetFloat("Speed", normalizedSpeed, 0.10f, Time.deltaTime);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isDodging", isDodging);
    }
}
