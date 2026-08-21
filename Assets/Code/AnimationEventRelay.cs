using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerAttack playerAttack;

    private void Awake()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();
    }

    // RPG Character Mecanim Animation Pack에 포함된 발소리 이벤트.
    public void FootL() { }
    public void FootR() { }

    // 2Hand-Sword-Attack 계열의 타격 프레임 이벤트.
    public void Hit()
    {
        if (playerAttack == null)
            playerAttack = GetComponentInParent<PlayerAttack>();

        if (playerAttack != null)
            playerAttack.OnAnimationHit();
    }

    // 일부 클립에서 사용할 수 있는 이벤트 이름들을 안전하게 받음.
    public void AttackEnd() { }
    public void CallAnimationEnd() { }
}
