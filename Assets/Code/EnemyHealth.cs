using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public Animator animator;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        Debug.Log($"{name} 피해 {amount} / 남은 HP {currentHealth}");

        if (currentHealth > 0)
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetTrigger("Die");
        else
            gameObject.SetActive(false);
    }
}
