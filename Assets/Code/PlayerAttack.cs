using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlayerAttack : MonoBehaviour
{
    private enum AttackMode
    {
        None,
        GroundCombo,
        DashAttack,
        JumpAttack
    }

    [Header("References")]
    public Animator animator;
    public GameObject weaponObject;

    [Header("Ground Combo")]
    public float attack1Duration = 0.62f;
    public float attack2Duration = 0.66f;
    public float attack3Duration = 0.82f;
    public float comboResetTime = 1.0f;
    [Range(0.1f, 0.9f)] public float comboQueueOpenRatio = 0.30f;

    [Header("Dash / Running Attack")]
    public float dashAttackDuration = 0.72f;
    public float dashAttackMoveDuration = 0.42f;
    public float dashAttackSpeed = 9.5f;
    public float dashAttackDamageMultiplier = 1.35f;

    [Header("Jump Slam Attack")]
    [Range(0f, 0.8f)] public float jumpAttackStartNormalized = 0.28f;
    public float jumpAttackHangTime = 0.18f;
    public float jumpAttackDownSpeed = 13.0f;
    public float jumpAttackMaxFallTime = 1.5f;
    public float jumpAttackLandingLock = 0.18f;
    public float jumpAttackDamageMultiplier = 1.65f;
    public float jumpAttackRadiusMultiplier = 1.25f;

    [Header("Guard")]
    [Range(0f, 0.95f)] public float guardDamageReduction = 0.80f;
    [Range(30f, 180f)] public float guardAngle = 120f;
    public float guardEndLockTime = 0.45f;

    [Header("Damage")]
    public int damage = 20;
    public float attackForwardOffset = 1.15f;
    public float attackRadius = 1.0f;
    public LayerMask targetLayers = ~0;

    [Header("Combat Stance")]
    public float combatIdleDuration = 6f;
    public float weaponShowDuration = 0.10f;
    public float weaponHideDuration = 0.30f;
    public float idleSpeedThreshold = 0.12f;

    [Header("Scene Rule")]
    public string dungeonSceneName = "NGF_CompactDungeon";

    [Header("Debug")]
    public bool showAttackGizmo = true;

    private Rigidbody rb;
    private PlayerMovement movement;

    private AttackMode attackMode = AttackMode.None;
    private bool isGuarding;
    private bool isGuardRecovering;

    private bool queuedAttack;
    private int comboStep;
    private float attackElapsed;
    private float currentAttackDuration;
    private float comboResetTimer;
    private bool hitApplied;

    private bool isCombatMode;
    private float combatIdleTimer;

    private Vector3 dashDirection = Vector3.forward;
    private float dashElapsed;

    private Coroutine weaponRoutine;
    private Coroutine guardRecoveryRoutine;
    private Vector3 weaponVisibleScale = Vector3.one;

    public bool IsAttacking => attackMode != AttackMode.None;
    public bool IsGuarding => isGuarding;
    public bool IsMovementLocked => IsAttacking || isGuarding || isGuardRecovering;
    public bool IsDashAttacking => attackMode == AttackMode.DashAttack;
    public bool IsCombatMode => isCombatMode;

    public Vector3 DashDirection => dashDirection;
    public float DashAttackSpeed => dashAttackSpeed;
    public float DashAttackMoveDuration => dashAttackMoveDuration;
    public float DashAttackElapsed => dashElapsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (weaponObject != null)
        {
            weaponVisibleScale = weaponObject.transform.localScale;
            weaponObject.SetActive(false);
        }

        SetCombat(false);
        SetGuard(false);
    }

    private void Update()
    {
        UpdateComboReset();

        if (attackMode == AttackMode.GroundCombo)
            attackElapsed += Time.deltaTime;

        if (attackMode == AttackMode.DashAttack)
            dashElapsed += Time.deltaTime;

        HandleGuardInput();

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleAttackInput();
        }

        UpdateCombatIdleTimer();
    }

    private void UpdateComboReset()
    {
        if (comboResetTimer <= 0f || IsAttacking)
            return;

        comboResetTimer -= Time.deltaTime;

        if (comboResetTimer <= 0f)
            comboStep = 0;
    }

    private void HandleGuardInput()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryStartGuard();
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            StopGuard();
        }
    }

    private void TryStartGuard()
    {
        if (IsAttacking || isGuarding || isGuardRecovering)
            return;

        bool grounded = movement == null || movement.IsGrounded;
        if (!grounded)
            return;

        EnterCombatMode();
        isGuarding = true;
        combatIdleTimer = 0f;
        comboResetTimer = 0f;
        comboStep = 0;

        StopHorizontalVelocity();
        SetGuard(true);

        if (animator != null)
        {
            animator.ResetTrigger("doGuardStart");
            animator.SetTrigger("doGuardStart");
        }
    }

    private void StopGuard()
    {
        if (!isGuarding)
            return;

        isGuarding = false;
        combatIdleTimer = 0f;
        SetGuard(false);

        if (guardRecoveryRoutine != null)
            StopCoroutine(guardRecoveryRoutine);

        guardRecoveryRoutine = StartCoroutine(GuardRecoveryRoutine());
    }

    private IEnumerator GuardRecoveryRoutine()
    {
        isGuardRecovering = true;

        if (guardEndLockTime > 0f)
            yield return new WaitForSeconds(guardEndLockTime);

        isGuardRecovering = false;
        guardRecoveryRoutine = null;
    }

    private void HandleAttackInput()
    {
        if (attackMode == AttackMode.JumpAttack ||
            attackMode == AttackMode.DashAttack)
        {
            return;
        }

        // 가드 중 좌클릭을 누르면 가드를 풀고 바로 공격으로 전환.
        if (isGuarding)
            StopGuard();

        bool grounded = movement == null || movement.IsGrounded;

        if (!grounded)
        {
            if (!IsAttacking)
                StartCoroutine(JumpAttackRoutine());

            return;
        }

        if (!IsAttacking &&
            movement != null &&
            movement.IsRunInput)
        {
            StartCoroutine(DashAttackRoutine());
            return;
        }

        EnterCombatMode();

        if (!IsAttacking)
        {
            int nextStep = comboStep + 1;

            if (nextStep < 1 || nextStep > 3)
                nextStep = 1;

            StartCoroutine(GroundAttackRoutine(nextStep));
            return;
        }

        if (attackMode == AttackMode.GroundCombo)
        {
            float queueOpenTime =
                currentAttackDuration * comboQueueOpenRatio;

            if (attackElapsed >= queueOpenTime &&
                comboStep < 3)
            {
                queuedAttack = true;
            }
        }
    }

    private IEnumerator GroundAttackRoutine(int step)
    {
        attackMode = AttackMode.GroundCombo;
        queuedAttack = false;
        comboStep = step;
        attackElapsed = 0f;
        combatIdleTimer = 0f;
        comboResetTimer = 0f;
        hitApplied = false;

        currentAttackDuration = GetAttackDuration(step);
        StopHorizontalVelocity();

        if (animator != null)
        {
            animator.SetInteger("attackIndex", step);
            animator.ResetTrigger("doAttack");
            animator.SetTrigger("doAttack");
        }

        float fallbackHitTime = currentAttackDuration * 0.55f;

        while (attackElapsed < currentAttackDuration)
        {
            StopHorizontalVelocity();

            if (!hitApplied && attackElapsed >= fallbackHitTime)
                ApplyCurrentAttackHit();

            yield return null;
        }

        attackMode = AttackMode.None;

        if (queuedAttack && comboStep < 3)
        {
            queuedAttack = false;
            StartCoroutine(GroundAttackRoutine(comboStep + 1));
        }
        else
        {
            comboResetTimer = comboResetTime;

            if (comboStep >= 3)
                comboStep = 0;
        }
    }

    private IEnumerator DashAttackRoutine()
    {
        EnterCombatMode();

        attackMode = AttackMode.DashAttack;
        queuedAttack = false;
        comboStep = 0;
        comboResetTimer = 0f;
        combatIdleTimer = 0f;
        hitApplied = false;
        dashElapsed = 0f;

        dashDirection = GetAttackForwardDirection();

        if (movement != null &&
            movement.CurrentMoveDirection.sqrMagnitude > 0.001f)
        {
            dashDirection = movement.CurrentMoveDirection.normalized;
        }

        FaceDirection(dashDirection);

        if (animator != null)
        {
            animator.ResetTrigger("doDashAttack");
            animator.SetTrigger("doDashAttack");
        }

        float fallbackHitTime = dashAttackDuration * 0.48f;

        while (dashElapsed < dashAttackDuration)
        {
            if (!hitApplied && dashElapsed >= fallbackHitTime)
                ApplyCurrentAttackHit();

            yield return null;
        }

        attackMode = AttackMode.None;
        comboResetTimer = comboResetTime;
    }

    private IEnumerator JumpAttackRoutine()
    {
        EnterCombatMode();

        attackMode = AttackMode.JumpAttack;
        queuedAttack = false;
        comboStep = 0;
        comboResetTimer = 0f;
        combatIdleTimer = 0f;
        hitApplied = false;

        StopHorizontalVelocity();

        // Mixamo Great Sword Jump Attack 전체 중 초반 재점프 구간을 건너뛰고
        // 내려찍기 준비 구간부터 시작한다.
        if (animator != null)
        {
            animator.CrossFade(
                "JumpAttack",
                0.03f,
                0,
                jumpAttackStartNormalized
            );
        }

        // 짧은 체공: 내려찍기 준비 자세를 보여준다.
        float hangTimer = 0f;
        while (hangTimer < jumpAttackHangTime)
        {
            hangTimer += Time.deltaTime;
            rb.linearVelocity = Vector3.zero;
            yield return null;
        }

        rb.linearVelocity = new Vector3(0f, -jumpAttackDownSpeed, 0f);

        float fallTimer = 0f;
        bool landed = false;

        while (fallTimer < jumpAttackMaxFallTime)
        {
            fallTimer += Time.deltaTime;

            if (movement != null && movement.IsGrounded)
            {
                landed = true;
                break;
            }

            yield return null;
        }

        if (landed)
        {
            ApplyDamage(
                jumpAttackDamageMultiplier,
                jumpAttackRadiusMultiplier
            );

            hitApplied = true;
            StopHorizontalVelocity();

            if (jumpAttackLandingLock > 0f)
                yield return new WaitForSeconds(jumpAttackLandingLock);
        }

        if (animator != null)
        {
            animator.CrossFade(
                "CombatLocomotion",
                0.06f,
                0
            );
        }

        attackMode = AttackMode.None;
        comboResetTimer = comboResetTime;
    }

    // AnimationEventRelay의 Hit()에서 호출.
    public void OnAnimationHit()
    {
        if (hitApplied)
            return;

        if (attackMode == AttackMode.GroundCombo ||
            attackMode == AttackMode.DashAttack)
        {
            ApplyCurrentAttackHit();
        }
    }

    private void ApplyCurrentAttackHit()
    {
        if (hitApplied)
            return;

        float multiplier;

        if (attackMode == AttackMode.GroundCombo)
        {
            multiplier = comboStep == 3 ? 1.35f :
                         comboStep == 2 ? 1.15f : 1.0f;
        }
        else if (attackMode == AttackMode.DashAttack)
        {
            multiplier = dashAttackDamageMultiplier;
        }
        else
        {
            return;
        }

        ApplyDamage(multiplier, 1f);
        hitApplied = true;
    }

    private float GetAttackDuration(int step)
    {
        if (step == 2) return attack2Duration;
        if (step == 3) return attack3Duration;
        return attack1Duration;
    }

    private void EnterCombatMode()
    {
        combatIdleTimer = 0f;

        if (!isCombatMode)
        {
            isCombatMode = true;
            SetCombat(true);
        }

        ShowWeapon();
    }

    private void UpdateCombatIdleTimer()
    {
        if (!isCombatMode || IsAttacking || isGuarding)
            return;

        Vector3 horizontal = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        if (horizontal.magnitude > idleSpeedThreshold)
        {
            combatIdleTimer = 0f;
            return;
        }

        combatIdleTimer += Time.deltaTime;

        if (combatIdleTimer >= combatIdleDuration)
        {
            combatIdleTimer = 0f;
            HideWeaponAndLeaveCombat();
        }
    }

    private void SetCombat(bool value)
    {
        if (animator != null)
            animator.SetBool("isCombat", value);
    }

    private void SetGuard(bool value)
    {
        if (animator != null)
            animator.SetBool("isGuarding", value);
    }

    private void ShowWeapon()
    {
        if (weaponObject == null)
            return;

        if (weaponRoutine != null)
            StopCoroutine(weaponRoutine);

        weaponRoutine = StartCoroutine(ScaleWeaponIn());
    }

    private IEnumerator ScaleWeaponIn()
    {
        weaponObject.SetActive(true);

        float duration = Mathf.Max(0.01f, weaponShowDuration);
        float t = 0f;
        weaponObject.transform.localScale = Vector3.zero;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            weaponObject.transform.localScale =
                Vector3.Lerp(Vector3.zero, weaponVisibleScale, p);

            yield return null;
        }

        weaponObject.transform.localScale = weaponVisibleScale;
        weaponRoutine = null;
    }

    private void HideWeaponAndLeaveCombat()
    {
        if (weaponRoutine != null)
            StopCoroutine(weaponRoutine);

        weaponRoutine = StartCoroutine(ScaleWeaponOut());
    }

    private IEnumerator ScaleWeaponOut()
    {
        if (weaponObject != null && weaponObject.activeSelf)
        {
            Vector3 start = weaponObject.transform.localScale;
            float duration = Mathf.Max(0.01f, weaponHideDuration);
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);

                weaponObject.transform.localScale =
                    Vector3.Lerp(start, Vector3.zero, p);

                yield return null;
            }

            weaponObject.SetActive(false);
            weaponObject.transform.localScale = weaponVisibleScale;
        }

        isCombatMode = false;
        SetCombat(false);
        weaponRoutine = null;
    }

    private Vector3 GetAttackForwardDirection()
    {
        Vector3 forward = transform.forward;

        if (animator != null)
            forward = animator.transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f || animator == null)
            return;

        animator.transform.rotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void StopHorizontalVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    public bool IsGuardingAgainst(Vector3 hitOrigin)
    {
        if (!isGuarding)
            return false;

        Vector3 toHit = hitOrigin - transform.position;
        toHit.y = 0f;

        if (toHit.sqrMagnitude < 0.001f)
            return true;

        toHit.Normalize();

        Vector3 forward = GetAttackForwardDirection();
        float halfAngle = guardAngle * 0.5f;
        float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        return Vector3.Dot(forward, toHit) >= minimumDot;
    }

    // 추후 PlayerHealth/적 공격 코드에서 호출하면 전방 가드 피해 감소가 적용된다.
    public int CalculateIncomingDamage(int incomingDamage, Vector3 hitOrigin)
    {
        if (!IsGuardingAgainst(hitOrigin))
            return incomingDamage;

        float remain = 1f - guardDamageReduction;
        return Mathf.Max(0, Mathf.CeilToInt(incomingDamage * remain));
    }

    private void ApplyDamage(float multiplier, float radiusMultiplier)
    {
        // 허브에서는 모션/가드/무기 연출만 재생하고 실제 공격 데미지는 없음.
        if (SceneManager.GetActiveScene().name != dungeonSceneName)
            return;

        Vector3 center = GetAttackCenter();

        Collider[] hits = Physics.OverlapSphere(
            center,
            attackRadius * radiusMultiplier,
            targetLayers,
            QueryTriggerInteraction.Ignore
        );

        HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || damaged.Contains(enemy))
                continue;

            damaged.Add(enemy);
            enemy.TakeDamage(Mathf.RoundToInt(damage * multiplier));
        }
    }

    private Vector3 GetAttackCenter()
    {
        Vector3 forward = GetAttackForwardDirection();

        return transform.position +
               Vector3.up * 0.9f +
               forward * attackForwardOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAttackGizmo)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackCenter(), attackRadius);
    }
}
