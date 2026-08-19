using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Orbit")]
    public float distance = 5f;
    public float minDistance = 1.2f;
    public float mouseSensitivity = 0.10f;
    public float yaw = 0f;
    public float pitch = 18f;
    public float minPitch = -10f;
    public float maxPitch = 65f;

    [Header("Collision")]
    public float collisionRadius = 0.20f;
    public float collisionPadding = 0.10f;
    public LayerMask collisionLayers = ~0;

    [Header("Editor Preview")]
    public bool previewInEditMode = true;

    private bool cursorLocked;
    private bool initialized;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[12];

    private void OnEnable()
    {
        TryFindTarget();

        if (!initialized && target != null)
        {
            yaw = target.eulerAngles.y;
            initialized = true;
        }

        if (!Application.isPlaying && previewInEditMode && target != null)
            PositionCamera(false);
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        TryFindTarget();
        if (target != null)
            yaw = target.eulerAngles.y;

        PositionCamera(true);
        SetCursorLock(true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetCursorLock(!cursorLocked);

        if (!cursorLocked || Mouse.current == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        // 비정상적으로 큰 마우스 델타가 한 프레임에 들어오는 것을 제한합니다.
        delta.x = Mathf.Clamp(delta.x, -80f, 80f);
        delta.y = Mathf.Clamp(delta.y, -80f, 80f);

        if (delta.sqrMagnitude > 0.01f)
        {
            yaw += delta.x * mouseSensitivity;
            pitch -= delta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void LateUpdate()
    {
        TryFindTarget();
        if (target == null)
            return;

        if (!Application.isPlaying)
        {
            if (previewInEditMode)
                PositionCamera(false);
            return;
        }

        PositionCamera(true);
    }

    private void PositionCamera(bool useCollision)
    {
        Vector3 focus = target.position + targetOffset;
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = orbit * Vector3.back;
        float finalDistance = distance;

        if (useCollision && distance > 0.01f)
        {
            // SphereCastAll 대신 NonAlloc을 써서 매 프레임 GC 할당을 만들지 않습니다.
            int hitCount = Physics.SphereCastNonAlloc(
                focus,
                collisionRadius,
                back,
                hitBuffer,
                distance,
                collisionLayers,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];
                if (hit.collider == null)
                    continue;

                Transform ht = hit.collider.transform;
                if (ht == target || ht.IsChildOf(target))
                    continue;

                float candidate = Mathf.Max(minDistance, hit.distance - collisionPadding);
                if (candidate < finalDistance)
                    finalDistance = candidate;
            }
        }

        Vector3 pos = focus + back * finalDistance;
        Quaternion rot = Quaternion.LookRotation(focus - pos, Vector3.up);
        transform.SetPositionAndRotation(pos, rot);
    }

    private void TryFindTarget()
    {
        if (target != null)
            return;

        GameObject p = GameObject.Find("Player");
        if (p != null)
            target = p.transform;
    }

    private void SetCursorLock(bool locked)
    {
        if (!Application.isPlaying)
            return;

        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
