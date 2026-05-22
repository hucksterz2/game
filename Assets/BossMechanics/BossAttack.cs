using System.Collections;
using UnityEngine;

public class BossAttack : MonoBehaviour
{
    [Header("Настройки")]
    public float chaseRange = 15f;
    public float stopDistance = 4f;
    public float attackRange = 5f;
    public float attackCooldown = 1.5f;
    public float moveSpeed = 3f;

    [Header("Границы арены")]
    public float arenaMinX = 40f;
    public float arenaMaxX = 70f;

    [Header("Снаряд")]
    public Sprite projectileSprite;
    public Sprite groundSpikeSprite;
    public float shootRange = 10f;
    public float shootCooldown = 3f;

    [Header("Игрок на боссе")]
    public float flashDuration = 1.2f;
    public float runAwaySpeed = 8f;

    [Header("Стан")]
    public float stunDuration = 2f;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private PlayerHealth playerHealth;
    private Rigidbody2D playerRb;
    private Transform playerTransform;
    private BossController bossController;

    private float lastAttackTime = -10f;
    private float lastShootTime = -10f;
    private bool isAttacking = false;
    private bool battleStarted = false;
    private bool isMoving = false;
    private float fixedY;

    private bool isFlashing = false;
    private bool isRunningAway = false;
    private float flashTimer = 0f;
    private float runAwayTargetX;

    private bool isStunned = false;
    private float stunTimer = 0f;

    private bool wasHitDuringAttack = false;
    private int runAwayCount = 0;

    private bool isPhase2Running = false;
    private int phase2SpikeCount = 0;

    private bool phase2MeleePhase = false;
    private float phase2MeleeTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
        bossController = GetComponent<BossController>();
        fixedY = transform.position.y;

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            playerHealth = ph;
            playerRb = ph.GetComponent<Rigidbody2D>();
            if (playerRb == null)
                playerRb = ph.GetComponentInParent<Rigidbody2D>();
            playerTransform = playerRb != null ? playerRb.transform : ph.transform;
        }
    }

    bool PlayerIsInAir()
    {
        if (playerRb == null) return false;
        return Mathf.Abs(playerRb.linearVelocity.y) > 1f;
    }

    bool PlayerIsOnBoss()
    {
        if (playerTransform == null || bossCollider == null) return false;
        float bossTop = bossCollider.bounds.max.y;
        float bossLeft = bossCollider.bounds.min.x;
        float bossRight = bossCollider.bounds.max.x;
        float playerX = playerTransform.position.x;
        float playerY = playerTransform.position.y;
        bool aboveBoss = playerY >= bossTop - 0.5f && playerY <= bossTop + 2f;
        bool withinWidth = playerX >= bossLeft && playerX <= bossRight;
        return aboveBoss && withinWidth;
    }

    float HorizontalDistance()
    {
        if (playerTransform == null) return 999f;
        return Mathf.Abs(playerTransform.position.x - transform.position.x);
    }

    float ClampToArena(float x)
    {
        return Mathf.Clamp(x, arenaMinX, arenaMaxX);
    }

    void Update()
    {
        if (playerTransform == null || playerHealth == null) return;

        if (!battleStarted)
        {
            if (HorizontalDistance() <= chaseRange)
            {
                battleStarted = true;
                if (bossController != null)
                    bossController.enabled = false;
            }
            return;
        }

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            return;
        }

        BossHealth health = GetComponent<BossHealth>();
        bool isPhase2 = health != null && health.currentHP <= health.maxHP * 0.5f;
        if (isPhase2)
        {
            if (isRunningAway) { RunAway(); return; }
            if (isFlashing) { UpdateFlash(); return; }
            if (PlayerIsOnBoss() && !isFlashing) { StartFlash(); return; }

            if (!isAttacking && !isPhase2Running)
            {
                if (phase2MeleePhase)
                {
                    phase2MeleeTimer -= Time.deltaTime;
                    float distP2 = HorizontalDistance();

                    if (phase2MeleeTimer <= 0f)
                    {
                        phase2MeleePhase = false;
                        StartCoroutine(Phase2Sequence());
                    }
                    else if (distP2 <= stopDistance + 2f)
                    {
                        StartAttack();
                    }
                    else
                    {
                        MoveToPlayer();
                    }
                }
                else
                {
                    if (Time.time >= lastAttackTime + attackCooldown * 0.5f)
                        StartCoroutine(Phase2Sequence());
                }
            }
            return;
        }

        if (isRunningAway)
        {
            RunAway();
            return;
        }

        if (PlayerIsOnBoss() && !isFlashing && !isRunningAway)
        {
            StartFlash();
            return;
        }

        if (isFlashing)
        {
            UpdateFlash();
            return;
        }

        if (PlayerIsInAir())
        {
            isMoving = false;
            animator.SetBool("isWalking", false);
            return;
        }

        float dist = HorizontalDistance();

        if (dist > shootRange && !isAttacking)
        {
            if (Time.time >= lastShootTime + shootCooldown)
                TriggerShoot();
            else
                StandIdle();
            return;
        }

        if (!isMoving && dist > stopDistance + 1f) isMoving = true;
        if (isMoving && dist <= stopDistance) isMoving = false;

        if (isMoving && !isAttacking)
            MoveToPlayer();
        else if (!isMoving && !isAttacking)
        {
            animator.SetBool("isWalking", false);
            if (Time.time >= lastAttackTime + attackCooldown)
                StartAttack();
        }
    }

    IEnumerator UndergroundAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("UnderAttack");

        yield return new WaitForSeconds(0.8f);

        float bossX = transform.position.x;
        float playerX = playerTransform.position.x;
        float dir = playerX > bossX ? 1f : -1f;

        for (int i = 0; i < 10; i++)
        {
            float spawnX = bossX + dir * (5.5f + i * 3.8f);
            spawnX = ClampToArena(spawnX);
            StartCoroutine(SpawnCreature(new Vector2(spawnX, fixedY), i * 0.1f));
        }

        yield return new WaitForSeconds(10 * 0.25f + 1.5f);
        isAttacking = false;
    }

    IEnumerator SpawnCreature(Vector2 pos, float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject c = new GameObject("GroundCreature");
        c.transform.position = pos + Vector2.down * 2f;
        c.transform.localScale = Vector3.one * 9f;

        SpriteRenderer sr = c.AddComponent<SpriteRenderer>();
        sr.sprite = groundSpikeSprite != null ? groundSpikeSprite : projectileSprite;
        sr.sortingLayerName = "Decorations";
        sr.sortingOrder = 0;

        Rigidbody2D rb = c.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        CircleCollider2D col = c.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.08f;
        col.enabled = false;

        float t = 0f;
        Vector3 from = c.transform.position;
        Vector3 to = (Vector3)pos + Vector3.up * 0f;
        while (t < 0.35f)
        {
            if (c == null) yield break;
            c.transform.position = Vector3.Lerp(from, to, t / 0.35f);
            t += Time.deltaTime;
            yield return null;
        }

        col.enabled = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        SpikeHit spike = c.AddComponent<SpikeHit>();
        spike.Init(playerHealth);

        yield return new WaitForSeconds(0.3f);

        float t2 = 0f;
        Vector3 currentPos = c.transform.position;
        Vector3 hidePos = currentPos + Vector3.down * 2f;
        while (t2 < 0.4f)
        {
            if (c == null) yield break;
            c.transform.position = Vector3.Lerp(currentPos, hidePos, t2 / 0.4f);
            t2 += Time.deltaTime;
            yield return null;
        }

        Destroy(c);
    }

    void StandIdle()
    {
        animator.SetBool("isWalking", false);
    }

    void TriggerShoot()
    {
        lastShootTime = Time.time;
        isAttacking = true;
        animator.SetBool("isWalking", false);
        spriteRenderer.flipX = playerTransform.position.x > transform.position.x;
        animator.SetTrigger("boltAttack");

        bool facingRight = spriteRenderer.flipX;
        float[] angles = { 70f, 35f, 0f, -35f, -70f };

        for (int i = 0; i < angles.Length; i++)
        {
            float delay = 0.5f + i * 0.15f;
            StartCoroutine(SpawnWithDelay(delay, angles[i], facingRight));
        }

        Invoke(nameof(EndAttack), 2f);
    }

    IEnumerator SpawnWithDelay(float delay, float angle, bool facingRight)
    {
        yield return new WaitForSeconds(delay);
        SpawnProjectile(angle, facingRight);
    }

    void SpawnProjectile(float angleDegrees, bool facingRight)
    {
        if (playerTransform == null || playerHealth == null) return;

        GameObject proj = new GameObject("BossProjectile");
        proj.transform.position = transform.position + new Vector3(
            facingRight ? 2f : -2f, 3f, 0);
        proj.transform.localScale = Vector3.one * 5f;

        SpriteRenderer sr = proj.AddComponent<SpriteRenderer>();
        sr.sprite = projectileSprite;
        sr.sortingOrder = 2;

        CircleCollider2D col = proj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.001f;
        col.enabled = false;

        Rigidbody2D rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        Vector2 dir = RotateVector(new Vector2(1f, 0f), angleDegrees);
        if (!facingRight)
            dir.x = -dir.x;

        BossProjectile bp = proj.AddComponent<BossProjectile>();
        bp.Init(dir, playerHealth, col);
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector2(
            v.x * Mathf.Cos(rad) - v.y * Mathf.Sin(rad),
            v.x * Mathf.Sin(rad) + v.y * Mathf.Cos(rad)
        );
    }

    void StartFlash()
    {
        isFlashing = true;
        flashTimer = flashDuration;
        isAttacking = false;
        CancelInvoke();
        animator.SetBool("isWalking", false);
        animator.SetTrigger("hurt");
    }

    void UpdateFlash()
    {
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            isFlashing = false;
            StartRunAway();
        }
    }

    void StartRunAway()
    {
        isRunningAway = true;
        isMoving = false;
        runAwayCount++;

        float distToLeft = transform.position.x - arenaMinX;
        float distToRight = arenaMaxX - transform.position.x;
        float directionAway = distToRight > distToLeft ? 1f : -1f;

        if (runAwayCount % 2 == 0)
        {
            float raw = transform.position.x + directionAway * 999f;
            runAwayTargetX = Mathf.Clamp(raw, arenaMinX + 5f, arenaMaxX - 5f);
        }
        else
        {
            float raw = transform.position.x + directionAway * (stopDistance + 7f);
            runAwayTargetX = ClampToArena(raw);
        }
    }

    void RunAway()
    {
        if (runAwayCount % 2 == 0 && HorizontalDistance() <= stopDistance + 1f && !PlayerIsInAir())
        {
            if (Time.time >= lastAttackTime + 0.5f)
            {
                lastAttackTime = Time.time;
                if (playerHealth != null)
                    playerHealth.TakeDamage(3);
            }
        }

        float dir = runAwayTargetX > transform.position.x ? 1f : -1f;
        Vector3 pos = transform.position;
        pos.x += dir * runAwaySpeed * Time.deltaTime;
        pos.x = ClampToArena(pos.x);
        pos.y = fixedY;
        transform.position = pos;

        spriteRenderer.flipX = dir > 0;
        animator.SetBool("isWalking", true);

        if (pos.x <= arenaMinX || pos.x >= arenaMaxX)
        {
            isStunned = true;
            stunTimer = stunDuration;
            isRunningAway = false;
            animator.SetBool("isWalking", false);
            return;
        }

        if (Mathf.Abs(transform.position.x - runAwayTargetX) < 0.5f)
        {
            animator.SetBool("isWalking", false);
            isRunningAway = false;
            isMoving = false;
        }
    }

    void MoveToPlayer()
    {
        float dir = playerTransform.position.x > transform.position.x ? 1f : -1f;
        Vector3 pos = transform.position;
        pos.x += dir * moveSpeed * Time.deltaTime;
        pos.x = ClampToArena(pos.x);
        pos.y = fixedY;
        transform.position = pos;

        spriteRenderer.flipX = dir > 0;
        animator.SetBool("isWalking", true);

        if (pos.x <= arenaMinX || pos.x >= arenaMaxX)
        {
            isStunned = true;
            stunTimer = stunDuration;
            isMoving = false;
            animator.SetBool("isWalking", false);
        }
    }

    void StartAttack()
    {
        lastAttackTime = Time.time;
        isAttacking = true;
        wasHitDuringAttack = false;
        animator.SetBool("isWalking", false);
        spriteRenderer.flipX = playerTransform.position.x > transform.position.x;
        animator.SetTrigger("attack");
        StartCoroutine(DealDamageAtFrame(0.88f));
        Invoke(nameof(EndAttack), 1f);
    }

    IEnumerator DealDamageAtFrame(float delay)
    {
        float elapsed = 0f;

        while (elapsed < delay)
        {
            if (isFlashing || isStunned || wasHitDuringAttack)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (isFlashing || isStunned || wasHitDuringAttack) yield break;

        if (playerHealth != null)
        {
            float yDiff = playerTransform.position.y - transform.position.y;
            if (yDiff <= 3f && Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
            {
                int damage = Random.Range(10, 31);
                playerHealth.TakeDamage(damage);
            }
        }
    }

    public void NotifyHit()
    {
        wasHitDuringAttack = true;
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    IEnumerator Phase2Sequence()
    {
        isPhase2Running = true;
        lastAttackTime = Time.time;

        float distToLeft = transform.position.x - arenaMinX;
        float distToRight = arenaMaxX - transform.position.x;
        float directionAway = distToRight > distToLeft ? 1f : -1f;
        float raw = transform.position.x + directionAway * 999f;
        runAwayTargetX = Mathf.Clamp(raw, arenaMinX + 5f, arenaMaxX - 5f);
        isRunningAway = true;

        while (isRunningAway)
            yield return null;

        for (int i = 0; i < 2; i++)
        {
            float dist = HorizontalDistance();
            if (dist <= stopDistance + 2f)
            {
                StartAttack();
                yield return new WaitForSeconds(1.2f);
            }
            else
            {
                yield return StartCoroutine(UndergroundAttack());
                yield return new WaitForSeconds(0.2f);
            }
        }

        isPhase2Running = false;
        lastAttackTime = Time.time;
        phase2MeleePhase = true;
        phase2MeleeTimer = 5f;
    }
}