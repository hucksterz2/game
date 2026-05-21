using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public Key attackKey = Key.J;
    public bool useMouseAttack = true;
    public float attackRadius = 2.5f;
    public int damage = 10;
    public float cooldown = 0.5f;

    [Header("Attack Position")]
    public Vector2 attackOffset = new Vector2(1.5f, 0f);

    [Header("Visualization")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.5f);
    public bool alwaysShowGizmo = true;

    [Header("Animator")]
    public Animator playerAnimator;

    private float lastAttackTime = -999f;

    void Start()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        bool keyPressed = Keyboard.current != null &&
                         Keyboard.current[attackKey].wasPressedThisFrame;
        bool mousePressed = useMouseAttack && Mouse.current != null &&
                   Mouse.current.rightButton.wasPressedThisFrame;

        if (keyPressed || mousePressed)
            TryAttack();
    }

    void TryAttack()
    {
        if (Time.time - lastAttackTime < cooldown) return;
        lastAttackTime = Time.time;

        if (playerAnimator != null)
            playerAnimator.SetTrigger("Attack");

        Vector2 attackPoint = GetAttackPoint();
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint, attackRadius);

        foreach (var hit in hits)
        {
            BossHealth boss = hit.GetComponent<BossHealth>();
            if (boss != null)
                boss.TakeDamage(damage);
        }
    }

    Vector2 GetAttackPoint()
    {
        float direction = transform.localScale.x > 0 ? -1f : 1f;
        return (Vector2)transform.position +
               new Vector2(attackOffset.x * direction, attackOffset.y);
    }

    void OnDrawGizmos()
    {
        if (!alwaysShowGizmo) return;
        DrawAttackGizmo();
    }

    void OnDrawGizmosSelected()
    {
        DrawAttackGizmo();
    }

    void DrawAttackGizmo()
    {
        Vector2 attackPoint = GetAttackPoint();
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(attackPoint, attackRadius);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.15f);
        Gizmos.DrawSphere(attackPoint, attackRadius);
    }
}