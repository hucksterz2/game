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
            playerTransform = ph.transform;
            playerRb = ph.GetComponent<Rigidbody2D>();
            if (playerRb == null)
                playerRb = ph.GetComponentInParent<Rigidbody2D>();
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
        col.radius = 0.3f;
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
        float directionAway = playerTransform.position.x > transform.position.x ? -1f : 1f;
        float target = transform.position.x + directionAway * 999f;
        runAwayTargetX = ClampToArena(target);
    }

    void RunAway()
    {
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
            isMoving = true;
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
            if (yDiff <= 1f && Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
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
}