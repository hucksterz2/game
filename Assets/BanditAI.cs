using Cainos.LucidEditor;
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
    public float hurtDuration      = 0.45f;

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
    private bool isHoldingPosition;

    [Header("Осведомленность")]
    public bool isAware = false;
    public float awarenessRadius = 35f;
    private float confidenceScale = 1f;

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
        if (isAware)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if(sr != null)
            {
                Color awareTint = new Color(1f, 0.92f, 0.75f);
                sr.color = Color.Lerp(sr.color, awareTint, Time.deltaTime * 2f);
            }
        }

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
            int chaseDir = player.position.x > transform.position.x ? 1 : -1;
            direction = chaseDir;

            float preferred = GetPreferredDistance();
            float hdist = Mathf.Abs(transform.position.x - player.position.x);

            if (preferred > 0f)
            {
                int awayDir = -chaseDir;

                if (hdist < preferred - 0.3f)
                {
                    isHoldingPosition = false;
                    stuckTimer = 0f;
                    rb.linearVelocity = new Vector2(currentSpeed * awayDir, rb.linearVelocity.y);
                }
                else if (hdist > preferred + 0.3f)
                {
                    isHoldingPosition = false;
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.25f)
                    {
                        if (Mathf.Abs(transform.position.x - lastPosX) < 0.05f)
                            rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
                        lastPosX = transform.position.x;
                        stuckTimer = 0f;
                    }
                    rb.linearVelocity = new Vector2(currentSpeed * chaseSpeedMult * chaseDir, rb.linearVelocity.y);
                }
                else
                {
                    isHoldingPosition = true;
                    stuckTimer = 0f;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.85f, rb.linearVelocity.y);
                }
            }
            else
            {
                isHoldingPosition = false;
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= 0.25f)
                {
                    if (Mathf.Abs(transform.position.x - lastPosX) < 0.05f)
                        rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
                    lastPosX = transform.position.x;
                    stuckTimer = 0f;
                }
                rb.linearVelocity = new Vector2(currentSpeed * chaseSpeedMult * chaseDir, rb.linearVelocity.y);
            }

            if (!isAttacking)
                transform.localScale = new Vector3(-chaseDir * baseScaleX, baseScaleY, baseScaleZ);
        }

        else
        {
            isHoldingPosition = false;
            stuckTimer = 0f;
            lastPosX = transform.position.x;
            turnCooldown = Mathf.Max(0f, turnCooldown - Time.deltaTime);

            bool groundAhead = (groundLayer == 0) || CheckGroundAhead();
            bool wallAhead = (groundLayer != 0) && CheckWallAhead();

            float dx = transform.position.x - startPos.x;
            bool shouldTurn = false;
            if (dx > patrolDistance && direction == 1) shouldTurn = true;
            if (dx < -patrolDistance && direction == -1) shouldTurn = true;
            if (turnCooldown <= 0f && (!groundAhead || wallAhead)) shouldTurn = true;

            if (shouldTurn && turnCooldown <= 0f)
            { direction *= -1; isTurning = true; turnCooldown = 0.6f; Invoke(nameof(ResetTurn), 0.3f); }

            rb.linearVelocity = new Vector2(currentSpeed * direction, rb.linearVelocity.y);
            if (!isAttacking)
                transform.localScale = new Vector3(-direction * baseScaleX, baseScaleY, baseScaleZ);
        }

        SetAnim(isAttacking ? ANIM_ATTACK : isHoldingPosition ? ANIM_ATTACK : ANIM_RUN);
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

    public void BecomeAware()
    {
        if (isAware) return;
        isAware = true;
        confidenceScale = 0.5f;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) StartCoroutine(AwarenessFlash(sr));
    }

    System.Collections.IEnumerator AwarenessFlash(SpriteRenderer sr)
    {
        Color orig = sr.color;
        for (int i = 0; i < 2; i++) 
        {
            sr.color = new Color(1f, 0.85f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            if (sr != null) sr.color = orig;
            yield return new WaitForSeconds(0.1f);
        }
    }

    float GetPreferredDistance()
    {
        if (!BanditMemory.HasKnowledge) return 0f;

        switch (BanditMemory.MostCommonAttack)
        {
            case PlayerAttackType.Combo1:
            case PlayerAttackType.Combo2:
            case PlayerAttackType.Combo3:
                return 3.8f;
            case PlayerAttackType.Dash:
                return 5.5f;
            case PlayerAttackType.Plunge:
                return 1.2f;
            default:
                return 0f;
        }
    }

    bool HasSpaceBehind(int awayDir)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.15f;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(awayDir, 0), 1.5f, groundLayer);
        return hit.collider == null;
    }

    public void Stun(float duration)
    {
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null && bar.IsDead) return;

        isStunned = true;
        stunTimer = duration;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        SetAnim(ANIM_IDLE);
    }

    public void StunWithKnockback(float duration, Vector2 velocity)
    {
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

        if (bar != null) bar.TakeDamage(amount);
        else
        {
            if (!isAttacking) TriggerAnim("Hurt");
        }

        Vector3   pos    = transform.position + Vector3.up * 0.6f;
        Transform parent = (bar != null && bar.IsDead) ? null : transform;
        BloodParticles.Spawn(pos, amount, parent);
        DamageNumber.Show(amount, pos + Vector3.up * 0.6f);
    }

    public void TakeDamageFromPlayer(int amount, PlayerAttackType type, ActionSnapshot snap, PlayerController player)
    {
        EnemyHealthBar bar = GetComponent<EnemyHealthBar>();
        if (bar != null && bar.IsDead) return;

        float confidence = BanditMemory.GetParryConfidence(type, snap);

        if (confidence > 0.2f && Random.value < confidence)
        {
            int reflected = Mathf.Max(1, amount / 2);
            PlayerHealth ph = player != null ? player.GetComponent<PlayerHealth>() : null;
            if (ph == null) ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null) ph.TakeDamage(reflected);

            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) StartCoroutine(ParryFlash(sr));
            return;
        }
        else if (confidence > 0.4f)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) StartCoroutine(FlinchFlash(sr));
        }

        TakeDamage(amount);
    }

    System.Collections.IEnumerator FlinchFlash(SpriteRenderer sr)
    {
        Color orig = sr.color;
        sr.color = new Color(1f, 1f, 0.6f);
        yield return new WaitForSeconds(0.06f);
        if (sr != null) sr.color = orig;
    }
        System.Collections.IEnumerator ParryFlash(SpriteRenderer sr)
    {
        Color orig = sr.color;
        sr.color = new Color(1f, 0.95f, 0.2f);
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = orig;
    }

    public void OnHurt()
    {
        if (!isAttacking)
        {
            isHurt    = true;
            hurtTimer = hurtDuration;
            TriggerAnim("Hurt");
        }
    }

    public void OnDeath()
    {
        TriggerAnim("Die");
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
