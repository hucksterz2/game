using UnityEngine;

public class BanditAI : MonoBehaviour
{
    [Header("Патруль")]
    public float speed               = 2f;
    public float patrolDistance      = 3f;
    public float groundCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [Header("Агро / Преследование")]
    public float aggroRange = 6f;

    [Header("Контактный толчок (OnCollision)")]
    public int damage = 1;

    [Header("Прицельная атака")]
    public float attackRange       = 1.8f;
    public float attackVertical    = 2.5f;
    public int   attackDamage      = 15;
    public float attackCooldown    = 1.2f;
    public float chaseSpeedMult    = 1.25f;

    const int ANIM_IDLE   = 0;
    const int ANIM_ATTACK = 1;
    const int ANIM_RUN    = 2;

    private Vector2     startPos;
    private int         direction = 1;
    private Rigidbody2D rb;
    private Animator    anim;
    private bool        isTurning;
    private bool        hasAnimStateParam;

    private bool  isStunned;
    private float stunTimer;

    private bool  isAttacking;
    private float attackAnimTimer;
    private float attackTimer;
    private float knockbackTimer;

    private float turnCooldown;

    private bool  isSlowed;
    private float slowTimer;

    private Transform    player;
    private PlayerHealth playerHealth;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        startPos = transform.position;

        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        if (anim != null)
            foreach (var p in anim.parameters)
                if (p.name == "AnimState" && p.type == AnimatorControllerParameterType.Int)
                { hasAnimStateParam = true; break; }

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player       = pc.transform;
            playerHealth = pc.GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = pc.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = pc.GetComponentInChildren<PlayerHealth>();
            if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (isSlowed) { slowTimer -= Time.deltaTime; if (slowTimer <= 0f) isSlowed = false; }

        if (isAttacking) { attackAnimTimer -= Time.deltaTime; if (attackAnimTimer <= 0f) isAttacking = false; }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            if (knockbackTimer > 0f) knockbackTimer -= Time.deltaTime;
            else rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnim(ANIM_IDLE);
            return;
        }

        if (player != null && attackTimer <= 0f && !isAttacking)
        {
            float hdist = Mathf.Abs(transform.position.x - player.position.x);
            float vdist = Mathf.Abs(transform.position.y - player.position.y);
            if (hdist <= attackRange && vdist <= attackVertical)
            {
                PerformMeleeAttack();
                return;
            }
        }

        if (isTurning)
        {
            float curSpd = isSlowed ? speed * 0.4f : speed;
            rb.linearVelocity    = new Vector2(curSpd * direction, rb.linearVelocity.y);
            transform.localScale = new Vector3(-direction, 1, 1);
            SetAnim(ANIM_RUN);
            return;
        }

        bool playerNearby = player != null
            && Mathf.Abs(transform.position.x - player.position.x) <= aggroRange
            && Mathf.Abs(transform.position.y - player.position.y) <= 2.5f;

        float currentSpeed = isSlowed ? speed * 0.4f : speed;

        if (playerNearby)
        {
            int chaseDir = player.position.x > transform.position.x ? 1 : -1;
            direction = chaseDir;

            if (!CheckWallAhead())
                rb.linearVelocity = new Vector2(currentSpeed * chaseSpeedMult * chaseDir, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            transform.localScale = new Vector3(-chaseDir, 1, 1);
        }
        else
        {
            turnCooldown = Mathf.Max(0f, turnCooldown - Time.deltaTime);

            bool groundAhead = (groundLayer == 0) || CheckGroundAhead();
            bool wallAhead   = (groundLayer != 0) && CheckWallAhead();

            float dx = transform.position.x - startPos.x;
            bool shouldTurn = false;
            if (dx >  patrolDistance && direction ==  1) shouldTurn = true;
            if (dx < -patrolDistance && direction == -1) shouldTurn = true;
            if (turnCooldown <= 0f && (!groundAhead || wallAhead)) shouldTurn = true;

            if (shouldTurn && turnCooldown <= 0f)
            { direction *= -1; isTurning = true; turnCooldown = 0.6f; Invoke(nameof(ResetTurn), 0.3f); }

            rb.linearVelocity = new Vector2(currentSpeed * direction, rb.linearVelocity.y);
            transform.localScale = new Vector3(-direction, 1, 1);
        }

        SetAnim(isAttacking ? ANIM_ATTACK : ANIM_RUN);
    }

    void PerformMeleeAttack()
    {
        if (player != null)
        {
            float hd = Mathf.Abs(transform.position.x - player.position.x);
            float vd = Mathf.Abs(transform.position.y - player.position.y);
            if (hd > attackRange || vd > attackVertical)
            { attackTimer = 0.15f; return; }
        }

        isAttacking     = true;
        attackAnimTimer = 0.5f;
        attackTimer     = attackCooldown;

        if (player != null)
        {
            float facing = (player.position.x - transform.position.x) > 0f ? -1f : 1f;
            transform.localScale = new Vector3(facing, 1, 1);
        }

        SetAnim(ANIM_ATTACK);

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>()
                        ?? player.GetComponentInChildren<PlayerHealth>()
                        ?? player.GetComponentInParent<PlayerHealth>();
        }

        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 kDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                kDir = new Vector2(kDir.x * 1.2f, 0.7f).normalized;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.AddForce(kDir * 12f, ForceMode2D.Impulse);
            }
        }
        if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
    }

    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnim(ANIM_IDLE);
    }

    public void StunWithKnockback(float duration, Vector2 velocity)
    {
        isStunned      = true;
        stunTimer      = duration;
        knockbackTimer = 0.2f;
        rb.linearVelocity = velocity;
        SetAnim(ANIM_IDLE);
    }

    public void TakeDamage(int amount)
    {
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null) bar.TakeDamage(amount);

        BloodParticles.Spawn(transform.position + Vector3.up * 0.6f, amount, transform);
        DamageNumber.Show(amount, transform.position + Vector3.up * 1.2f);
    }

    public void ApplySlow(float duration)
    {
        isSlowed  = true;
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    void SetAnim(int state)
    {
        if (anim != null && hasAnimStateParam)
            anim.SetInteger("AnimState", state);
    }

    void ResetTurn() => isTurning = false;

    bool CheckGroundAhead()
    {
        Vector2 checkPos = (Vector2)transform.position + new Vector2(direction * 0.4f, -0.8f);
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(checkPos, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    bool CheckWallAhead()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.15f;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0), 0.35f, groundLayer);
        Debug.DrawRay(origin, new Vector2(direction * 0.35f, 0), hit.collider != null ? Color.yellow : Color.cyan);
        return hit.collider != null;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (isStunned) return;
        if (col.gameObject.CompareTag("Player"))
        {
            isAttacking     = true;
            attackAnimTimer = 0.4f;

            Rigidbody2D playerRb = col.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockDir = (col.transform.position - transform.position).normalized;
                knockDir = new Vector2(knockDir.x * 1.5f, 1f).normalized;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.AddForce(knockDir * 18f, ForceMode2D.Impulse);
            }
            PlayerHealth ph = col.gameObject.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }
    }
}
