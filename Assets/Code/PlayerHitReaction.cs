using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHitReaction : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Hit Reaction")]
    [Tooltip("피격 모션 동안 이동 입력을 잠깐 잠급니다.")]
    public float hitLockDuration = 0.50f;

    [Header("Temporary Test")]
    [Tooltip("Enemy가 아직 없을 때 H 키로 피격 모션을 테스트합니다.")]
    public bool enableHKeyTest = true;

    private Rigidbody rb;
    private Behaviour movementBehaviour;
    private Coroutine hitRoutine;
    private bool isReacting;

    public bool IsReacting => isReacting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        // PlayerMovement에 직접 강하게 의존하지 않도록 이름으로 찾습니다.
        movementBehaviour = GetComponent("PlayerMovement") as Behaviour;
    }

    private void Update()
    {
        if (!enableHKeyTest || Keyboard.current == null)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
            PlayHitReaction();
    }

    // 나중에 Enemy 공격 코드에서 이 함수만 호출하면 됩니다.
    public void ReceiveHit()
    {
        PlayHitReaction();
    }

    public void PlayHitReaction()
    {
        if (!isActiveAndEnabled || animator == null)
            return;

        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        isReacting = true;

        bool movementWasEnabled =
            movementBehaviour != null && movementBehaviour.enabled;

        if (movementBehaviour != null)
            movementBehaviour.enabled = false;

        StopHorizontalVelocity();

        animator.ResetTrigger("doHit");
        animator.SetTrigger("doHit");

        float timer = 0f;
        float duration = Mathf.Max(0.05f, hitLockDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            StopHorizontalVelocity();
            yield return null;
        }

        if (movementBehaviour != null)
            movementBehaviour.enabled = movementWasEnabled;

        isReacting = false;
        hitRoutine = null;
    }

    private void StopHorizontalVelocity()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}
