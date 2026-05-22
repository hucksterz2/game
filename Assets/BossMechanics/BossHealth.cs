using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("HP Settings")]
    public int maxHP = 200;
    public int currentHP;

    [Header("References")]
    public Animator animator;

    private bool isDead = false;

    void Start()
    {
        currentHP = maxHP;
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);

        if (animator != null)
        {
            bool isPhase2 = currentHP <= maxHP * 0.5f;
            animator.SetBool("isPhase2", isPhase2);
        }

        BossAttack attack = GetComponent<BossAttack>();
        if (attack != null) attack.NotifyHit();

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("hurt");
    }

    void Die()
    {
        isDead = true;

        if (animator != null)
        {
            animator.ResetTrigger("attack");
            animator.ResetTrigger("hurt");
            animator.SetBool("isWalking", false);
            animator.SetTrigger("death");
        }

        BossController controller = GetComponent<BossController>();
        if (controller != null) controller.enabled = false;

        BossAttack attack = GetComponent<BossAttack>();
        if (attack != null) attack.enabled = false;

        BossDefeatTrigger trigger = GetComponent<BossDefeatTrigger>();
        if (trigger != null)
        {
            trigger.TriggerManually();
            return;
        }

        Destroy(gameObject, 2f);
    }

    public bool IsDead() => isDead;
}