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
    public float hurtDuration      = 0.45f;  // длительность анимации получения урона

    const int ANIM_IDLE   = 0;
    const int ANIM_ATTACK = 1;
    const int ANIM_RUN    = 2;

    private Vector2     startPos;
    private int         direction = 1;
    private Rigidbody2D rb;
    private Animator    anim;
    private bool        isTurning;
    private bool        hasAnimStateParam;
    private float       baseScaleX;
    private float       baseScaleY;
    private float       baseScaleZ;

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

    private float lastPosX;
    private float stuckTimer;
    private bool  touchingWall;

    private float pullTimer;

    private bool  isHurt;
    private float hurtTimer;

    void Start()
    {
        rb         = GetComponent<Rigidbody2D>();
        startPos   = transform.position;
        lastPosX   = transform.position.x;
        baseScaleX = Mathf.Abs(transform.localScale.x);
        baseScaleY = transform.localScale.y;
        baseScaleZ = transform.localScale.z;

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

        if (isHurt) { hurtTimer -= Time.deltaTime; if (hurtTimer <= 0f) isHurt = false; }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            if (knockbackTimer > 0f) knockbackTimer -= Time.deltaTime;
            else rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SetAnim(ANIM_IDLE);
            return;
        }

        if (pullTimer > 0f)
        {
            pullTimer -= Time.deltaTime;
            SetAnim(ANIM_IDLE);
            return;
        }

        if (player != null && attackTimer <= 0f && !isAttacking && !isHurt)
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
            rb.linearVelocity = new Vector2(curSpd * direction, rb.linearVelocity.y);
            if (!isAttacking)
                transform.localScale = new Vector3(-direction * baseScaleX, baseScaleY, baseScaleZ);
            SetAnim(ANIM_RUN);
            return;
        }

        bool playerNearby = player != null
            && Mathf.Abs(transform.position.x - player.position.x) <= aggroRange
            && Mathf.Abs(transform.position.y - player.position.y) <= 2.5f;

        float currentSpeed = isSlowed ? speed * 0.4f : speed;

        if (playerNearby)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= 0.25f)
            {
                if (Mathf.Abs(transform.position.x - lastPosX) < 0.05f)
                    rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
                lastPosX   = transform.position.x;
                stuckTimer = 0f;
            }

            int chaseDir = player.position.x > transform.position.x ? 1 : -1;
            direction = chaseDir;

            rb.linearVelocity = new Vector2(currentSpeed * chaseSpeedMult * chaseDir, rb.linearVelocity.y);

            if (!isAttacking)
                transform.localScale = new Vector3(-chaseDir * baseScaleX, baseScaleY, baseScaleZ);
        }
        else
        {
            stuckTimer = 0f;
            lastPosX   = transform.position.x;
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
            if (!isAttacking)
                transform.localScale = new Vector3(-direction * baseScaleX, baseScaleY, baseScaleZ);
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
            transform.localScale = new Vector3(facing * baseScaleX, baseScaleY, baseScaleZ);
        }

        SetAnim(ANIM_ATTACK);
        if (anim != null)
            foreach (var p in anim.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger &&
                    (p.name == "Attack" || p.name == "attack"))
                { anim.SetTrigger(p.name); break; }

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
        // Не перезаписывать состояние смерти (stunTimer = 999)
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null && bar.IsDead) return;

        isStunned = true;
        stunTimer = duration;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnim(ANIM_IDLE);
    }

    public void StunWithKnockback(float duration, Vector2 velocity)
    {
        // Не применять кикбэк и оглушение к мёртвым бандитам
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null && bar.IsDead) return;

        isStunned      = true;
        stunTimer      = duration;
        knockbackTimer = 0.2f;
        rb.linearVelocity = velocity;
        SetAnim(ANIM_IDLE);
    }

    public void TakeDamage(int amount)
    {
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null && bar.IsDead) return;

        // EnemyHealthBar.TakeDamage вызовет OnHurt/OnDeath сам через нотификацию
        if (bar != null) bar.TakeDamage(amount);
        else
        {
            // Если нет EnemyHealthBar — обработать анимацию здесь
            if (!isAttacking) TriggerAnim("Hurt");
        }

        Vector3   pos    = transform.position + Vector3.up * 0.6f;
        Transform parent = (bar != null && bar.IsDead) ? null : transform;
        BloodParticles.Spawn(pos, amount, parent);
        DamageNumber.Show(amount, pos + Vector3.up * 0.6f);
    }

    // Вызывается из EnemyHealthBar когда бандит получает урон (но не умирает)
    public void OnHurt()
    {
        if (!isAttacking)
        {
            isHurt    = true;
            hurtTimer = hurtDuration;
            TriggerAnim("Hurt");
        }
    }

    // Вызывается из EnemyHealthBar когда бандит умирает
    public void OnDeath()
    {
        TriggerAnim("Die");
        // Переводим в кинематику — игнорирует все силы и AddForce,
        // но прямое присваивание linearVelocity всё ещё работает,
        // поэтому обнуляем скорость сами
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;
        isStunned         = true;
        stunTimer         = 999f;
    }

    void TriggerAnim(string triggerName)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            { anim.SetTrigger(triggerName); return; }
    }

    public void Pull(Vector2 velocity)
    {
        pullTimer         = 0.15f;
        rb.linearVelocity = velocity;
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

    void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player")) return;
        foreach (ContactPoint2D c in col.contacts)
        {
            if (Mathf.Abs(c.normal.x) > 0.7f &&
                Mathf.Sign(-c.normal.x) == Mathf.Sign(direction))
            { touchingWall = true; return; }
        }
        touchingWall = false;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) touchingWall = false;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (isStunned) return;
        if (col.gameObject.CompareTag("Player"))
        {
            PlayerController pc = col.gameObject.GetComponent<PlayerController>();
            if (pc != null && pc.IsPlunging) return;

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
