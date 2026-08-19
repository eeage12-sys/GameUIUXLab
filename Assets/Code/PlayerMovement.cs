using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 32f;
    public float deceleration = 42f;

    [Header("Facing")]
    [Tooltip("캐릭터 모델(Animator가 붙은 자식)을 넣습니다. 비워두면 자동으로 찾습니다.")]
    public Transform visualRoot;
    public float visualTurnSpeed = 12f;
    [Tooltip("모델이 뒤를 보고 달리면 180으로 바꾸세요.")]
    public float modelYawOffset = 0f;

    [Header("Camera")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 desiredFacing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 물리 충돌이 Player 본체를 빙글빙글 돌리지 못하도록 모든 회전을 잠급니다.
        rb.constraints |= RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (visualRoot == null)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
                visualRoot = anim.transform;
        }
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        ReadKeyboard();

        // Rigidbody 본체는 회전시키지 않고, 눈에 보이는 모델만 부드럽게 회전시킵니다.
        if (visualRoot != null && desiredFacing.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(desiredFacing, Vector3.up)
                              * Quaternion.Euler(0f, modelYawOffset, 0f);

            visualRoot.rotation = Quaternion.Slerp(
                visualRoot.rotation,
                look,
                1f - Mathf.Exp(-visualTurnSpeed * Time.deltaTime)
            );
        }
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null)
            return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        Vector3 currentPlanar = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetPlanar = moveDirection * moveSpeed;

        float rate = moveDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        Vector3 nextPlanar = Vector3.MoveTowards(
            currentPlanar,
            targetPlanar,
            rate * Time.fixedDeltaTime
        );

        // 중력 Y 속도는 그대로 유지합니다.
        rb.linearVelocity = new Vector3(nextPlanar.x, rb.linearVelocity.y, nextPlanar.z);

        if (moveDirection.sqrMagnitude > 0.0001f)
            desiredFacing = moveDirection.normalized;
    }

    private void ReadKeyboard()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;

        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }
}
