using UnityEngine;

public class BanditAI : MonoBehaviour
{
    public float speed = 2f;
    public float patrolDistance = 3f;
    public int damage = 1;
    public float knockbackForce = 18f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    private Vector2 startPos;
    private int direction = 1;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isTurning = false;

    const int ANIM_IDLE = 0;
    const int ANIM_RUN = 2;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;

        anim = GetComponent<Animator>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        if (anim == null)
            Debug.LogError("Бандит: Animator не найден нигде!");
        else
            Debug.Log("Бандит: Animator найден на " + anim.gameObject.name);
    }

    void Update()
    {
        if (isTurning)
        {
            SetAnim(ANIM_IDLE);
            return;
        }

        bool groundAhead = CheckGroundAhead();
        float dist = transform.position.x - startPos.x;

        bool shouldTurn = false;
        if (dist > patrolDistance && direction == 1) shouldTurn = true;
        if (dist < -patrolDistance && direction == -1) shouldTurn = true;
        if (!groundAhead) shouldTurn = true;

        if (shouldTurn)
        {
            direction *= -1;
            isTurning = true;
            Invoke(nameof(ResetTurn), 0.2f);
        }

        rb.linearVelocity = new Vector2(speed * direction, rb.linearVelocity.y);
        transform.localScale = new Vector3(-direction, 1, 1);

        SetAnim(ANIM_RUN);
    }

    void SetAnim(int state)
    {
        if (anim != null)
            anim.SetInteger("AnimState", state);
    }

    void ResetTurn() => isTurning = false;

    bool CheckGroundAhead()
    {
        Vector2 checkPos = (Vector2)transform.position
            + new Vector2(direction * 0.4f, -0.8f);
        RaycastHit2D hit = Physics2D.Raycast(
            checkPos, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(checkPos, Vector2.down * groundCheckDistance,
            hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockDir = (col.transform.position
                    - transform.position).normalized;
                knockDir = new Vector2(knockDir.x * 1.5f, 1f).normalized;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.AddForce(knockDir * 18f, ForceMode2D.Impulse);
            }

            PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }
    }

    public void TakeDamage(int amount)
    {
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null) bar.TakeDamage(amount);
    }
}