using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float speed = 5f;

    [Header("Прыжок")]
    public float jumpForce = 20f;
    public int maxJumps = 2;
    public float groundCheckRadius = 0.2f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Dash")]
    public float dashSpeed    = 18f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public int   dashDamage   = 10;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 1.5f;
    public float wallJumpForceX = 8f;
    public float wallJumpForceY = 12f;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public float wallCheckDist = 0.15f;

    [Header("Ladder")]
    public float climbSpeed = 4f;
    public LayerMask ladderLayer;

    [Header("Комбо-атака (F)")]
    public float   attackRange    = 1.4f;
    public float   attackCooldown = 0.25f;
    public float   comboWindow    = 0.8f;
    public int[]   comboDamage    = { 8, 13, 20 };
    public float[] comboRangeX    = { 3.2f, 2.4f, 3.2f };
    public float[] comboRangeY    = { 1.4f, 2.0f, 1.6f };
    public float[] comboOffsetX   = { 1.7f, 1.1f, 1.4f };
    public float[] comboOffsetY   = { 0.0f, -0.1f, 0.0f };

    [Header("Блок / Парирование (Q)")]
    public float blockDamageReduction = 0.5f;
    public float parryWindow          = 0.2f;
    public float parryStunDuration    = 1.5f;
    public float parryCooldown        = 0.8f;
    public int   parryReflectDamage   = 20;

    [Header("Урон от падения")]
    public float fallDamageThreshold  = 17f;
    public float fallDamageMultiplier = 1.5f;

    [Header("Атака сверху (E)")]
    public int   plungeDamage     = 25;
    public float plungeSpeed      = 22f;
    public float plungeBounceY    = 14f;
    public float plungeBounceX    = 8f;
    public float plungeDetectDist = 3.5f;

    public bool IsBlocking      { get; private set; }
    public bool ParryActive     { get; private set; }
    public bool IsDashInvincible{ get; private set; }
    public bool IsDead          { get; set; }
    public bool IsPlunging      => isPlunging || plungeGraceTimer > 0f;

    private Rigidbody2D rb;
    private Animator    anim;
    private int         jumpsLeft;
    private Collider2D  playerCol;

    private float baseScaleX, baseScaleY, baseScaleZ;

    private bool  isOnLadder, isClimbing;
    private float originalGravityScale;
    private System.Collections.Generic.List<Collider2D> ignoredCeiling =
        new System.Collections.Generic.List<Collider2D>();

    private float coyoteTimer, jumpBufferTimer;
    private bool  wasGrounded;

    private bool  isDashing;
    private float dashTimer, dashCooldownTimer, dashDirection;
    private bool  dashThroughUsed;

    private float attackCooldownTimer;
    private int   comboStep;
    private float comboResetTimer;

    private float parryTimer;
    private float parryCooldownTimer;

    private bool isTouchingWall, isWallSliding, wasTouchingWall;
    private int  wallDirection;

    private bool            isPlunging;
    private bool            plungeAnimTriggered;
    private float           plungeStartDist;
    private float           plungeTimer;
    private float           plungeGraceTimer;
    private BanditAI        plungeTarget;
    private HeavyBanditBoss plungeTargetBoss;
    private BossHealth      plungeTargetBossHealth;
    private HeavyBanditBoss cachedBoss;
    private BossHealth      cachedBossHealth;
    private GameObject      plungePromptGO;

    private float stuckTimer;
    private float lastInputX;

    private RageSystem rageSystem;
    private SpriteRenderer playerSprite;
    private float startupDelay = 0.15f;
    private float maxFallVelocity;

    private float lastDashTime = -10f;
    private float lastJumpTime = -10f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();

        Animator[] animators = GetComponentsInChildren<Animator>();
        if (animators.Length > 0) anim = animators[0];

        jumpsLeft            = maxJumps;
        originalGravityScale = rb.gravityScale;

        baseScaleX = Mathf.Abs(transform.localScale.x);
        baseScaleY = transform.localScale.y;
        baseScaleZ = transform.localScale.z;

        playerCol  = GetComponent<Collider2D>();
        if (playerCol == null) playerCol = GetComponentInParent<Collider2D>();

        rageSystem        = GetComponent<RageSystem>() ?? GetComponentInParent<RageSystem>();
        playerSprite      = GetComponentInChildren<SpriteRenderer>();
        cachedBoss        = FindFirstObjectByType<HeavyBanditBoss>();
        cachedBossHealth  = FindFirstObjectByType<BossHealth>();
        rb.linearVelocity = Vector2.zero;
    }

    void IgnoreCeilings()
    {
        if (playerCol == null || groundLayer == 0) return;
        Vector2 headPos = rb.position + Vector2.up * 1.1f;
        Collider2D[] above = Physics2D.OverlapCircleAll(headPos, 0.45f, groundLayer);
        foreach (var col in above)
            if (!ignoredCeiling.Contains(col))
            { Physics2D.IgnoreCollision(playerCol, col, true); ignoredCeiling.Add(col); }
    }

    void RestoreCeilings()
    {
        if (playerCol == null) return;
        foreach (var col in ignoredCeiling)
            if (col != null) Physics2D.IgnoreCollision(playerCol, col, false);
        ignoredCeiling.Clear();
    }

    void CheckLadder()
    {
        if (ladderLayer == 0) return;
        bool near = Physics2D.OverlapCircle(transform.position, 0.6f, ladderLayer) != null;
        if (near && !isOnLadder)
            isOnLadder = true;
        else if (!near && isOnLadder)
        { RestoreCeilings(); isOnLadder = false; isClimbing = false; rb.gravityScale = originalGravityScale; }
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.15f, groundLayer);
        Debug.DrawRay(groundCheck.position, Vector2.down * 0.15f, hit.collider != null ? Color.green : Color.red);
        return hit.collider != null;
    }

    bool IsTouchingWall(out int dir)
    {
        dir = 0;
        if (wallCheckLeft != null)
        { RaycastHit2D h = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckDist, groundLayer); if (h.collider != null) { dir = -1; return true; } }
        if (wallCheckRight != null)
        { RaycastHit2D h = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckDist, groundLayer); if (h.collider != null) { dir = 1; return true; } }
        return false;
    }

    void Update()
    {
        if (IsDead)
        {
            isPlunging             = false;
            plungeGraceTimer       = 0f;
            plungeTarget           = null;
            plungeTargetBoss       = null;
            plungeTargetBossHealth = null;
            return;
        }
        if (startupDelay > 0f) { startupDelay -= Time.deltaTime; rb.linearVelocity = Vector2.zero; return; }

        float dbgInputX = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  dbgInputX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) dbgInputX =  1f;

        if (dbgInputX != 0f && Mathf.Abs(transform.position.x - lastInputX) < 0.03f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 0.4f)
            {
                isPlunging          = false;
                plungeAnimTriggered = false;
                plungeGraceTimer    = 0f;
                plungeTarget        = null;
                plungeTargetBoss    = null;
                isDashing           = false;
                isClimbing          = false;
                isOnLadder          = false;
                ShowPlungePrompt(false);
                if (rb.gravityScale == 0f) rb.gravityScale = originalGravityScale;
                GrapplingHook.isSwinging = false;
                RestoreCeilings();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastInputX = transform.position.x;

        CheckLadder();

        if (plungeGraceTimer > 0f) plungeGraceTimer -= Time.deltaTime;
        attackCooldownTimer -= Time.deltaTime;
        if (comboResetTimer > 0f)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f) comboStep = 0;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame && attackCooldownTimer <= 0f && !isDashing && !isPlunging)
            PerformComboAttack();

        parryCooldownTimer -= Time.deltaTime;
        if (parryTimer > 0f)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0f) ParryActive = false;
        }
        if (Keyboard.current.qKey.wasPressedThisFrame && parryCooldownTimer <= 0f)
        {
            ParryActive = true;
            parryTimer  = parryWindow;
        }
        IsBlocking = Keyboard.current.qKey.isPressed;

        bool grounded = IsGrounded();

        if (!grounded && rb.linearVelocity.y < maxFallVelocity)
            maxFallVelocity = rb.linearVelocity.y;

        if (grounded)
        {
            coyoteTimer = coyoteTime;
            if (!wasGrounded)
            {
                jumpsLeft = maxJumps;
                if (maxFallVelocity < -fallDamageThreshold && !isPlunging)
                {
                    int dmg = Mathf.RoundToInt((-maxFallVelocity - fallDamageThreshold) * fallDamageMultiplier);
                    PlayerHealth ph = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
                    if (ph != null) ph.TakeDamage(dmg);
                }
                maxFallVelocity = 0f;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
        wasGrounded = grounded;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        isTouchingWall = IsTouchingWall(out wallDirection);
        isWallSliding  = isTouchingWall && !grounded && rb.linearVelocity.y < 0;
        if (isWallSliding) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        if (isTouchingWall && !wasTouchingWall && !grounded) jumpsLeft = maxJumps;
        wasTouchingWall = isTouchingWall;

        dashCooldownTimer -= Time.deltaTime;
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && dashCooldownTimer <= 0 && !isDashing && !isPlunging)
        {
            float move = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  move = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move =  1f;
            if (move == 0f) move = transform.localScale.x > 0 ? -1f : 1f;
            dashDirection = move; isDashing = true; dashTimer = dashDuration;
            lastDashTime = Time.time;
            dashCooldownTimer = dashCooldown; dashThroughUsed = false;
            IsDashInvincible = true;
            StartCoroutine(DashBlinkRoutine());
        }
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            DashThroughCheck();
            if (dashTimer <= 0) { isDashing = false; }
            return;
        }

        if (isPlunging)
        {
            if (plungeTarget == null && plungeTargetBoss == null && plungeTargetBossHealth == null)
            {
                isPlunging = false;
                plungeAnimTriggered = false;
                ShowPlungePrompt(false);
                if (rb.gravityScale == 0f) rb.gravityScale = originalGravityScale;
            }
            else
            {
                Transform targetTf = plungeTarget           != null ? plungeTarget.transform
                                   : plungeTargetBoss       != null ? plungeTargetBoss.transform
                                   : plungeTargetBossHealth.transform;

                float yVelBefore = rb.linearVelocity.y;
                plungeTimer += Time.deltaTime;
                float dist = Vector2.Distance(transform.position, targetTf.position);

                float curPlungeSpeed = dist < 2.2f ? plungeSpeed * 0.35f : plungeSpeed;
                rb.linearVelocity = new Vector2(0f, -curPlungeSpeed);

                if (!plungeAnimTriggered && (dist < plungeStartDist * 0.55f || plungeTimer > 0.18f))
                {
                    plungeAnimTriggered = true;
                    SafeSetTrigger("Plunge");
                }

                bool reachedTarget  = dist < 0.75f;
                bool stoppedFalling = plungeTimer > 0.15f && Mathf.Abs(yVelBefore) < 1f;
                bool timedOut       = plungeTimer > 1.5f;

                if (plungeTimer > 0.1f && (reachedTarget || grounded || stoppedFalling || timedOut))
                    FinishPlunge();
            }
            return;
        }

        if (GrapplingHook.isSwinging)
        {
            if (isClimbing) { isClimbing = false; rb.gravityScale = originalGravityScale; RestoreCeilings(); }
            if (!grounded)
            {
                if (rb.linearVelocity.x > 0.2f)       transform.localScale = new Vector3(-baseScaleX, baseScaleY, baseScaleZ);
                else if (rb.linearVelocity.x < -0.2f)  transform.localScale = new Vector3( baseScaleX, baseScaleY, baseScaleZ);
                if (anim != null) anim.SetFloat("MoveSpeed", 1f);
                return;
            }
        }

        if (!grounded && rb.linearVelocity.y < -1.5f && plungeTarget == null && plungeTargetBoss == null)
        {
            Vector2 boxCenter = (Vector2)transform.position + Vector2.down * (plungeDetectDist * 0.5f + 0.5f);
            Collider2D[] belowHits = Physics2D.OverlapBoxAll(boxCenter, new Vector2(0.8f, plungeDetectDist), 0f);
            foreach (var col in belowHits)
            {
                BanditAI b = col.GetComponentInParent<BanditAI>();
                if (b != null) { plungeTarget = b; break; }
                HeavyBanditBoss boss = col.GetComponentInParent<HeavyBanditBoss>();
                if (boss != null && !boss.IsDead) { plungeTargetBoss = boss; break; }
                BossHealth bH = col.GetComponentInParent<BossHealth>();
                if (bH != null && !bH.IsDead()) { plungeTargetBossHealth = bH; break; }
            }
        }
        else if (grounded || rb.linearVelocity.y >= 0f)
        {
            if (plungeTarget           != null) { plungeTarget           = null; ShowPlungePrompt(false); }
            if (plungeTargetBoss       != null) { plungeTargetBoss       = null; ShowPlungePrompt(false); }
            if (plungeTargetBossHealth != null) { plungeTargetBossHealth = null; ShowPlungePrompt(false); }
        }

        ShowPlungePrompt(plungeTarget != null || plungeTargetBoss != null || plungeTargetBossHealth != null);

        if ((plungeTarget != null || plungeTargetBoss != null || plungeTargetBossHealth != null) && Keyboard.current.fKey.wasPressedThisFrame)
        {
            isPlunging          = true;
            plungeAnimTriggered = true;
            plungeTimer         = 0f;
            Transform ptf = plungeTarget           != null ? plungeTarget.transform
                          : plungeTargetBoss       != null ? plungeTargetBoss.transform
                          : plungeTargetBossHealth.transform;
            plungeStartDist = Vector2.Distance(transform.position, ptf.position);
            if (plungeStartDist < 0.1f) plungeStartDist = 3f;
            SafeSetTrigger("Plunge");
            ShowPlungePrompt(false);
            return;
        }

        if (isOnLadder)
        {
            float vIn = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)   vIn =  1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vIn = -1f;
            if (vIn != 0f && !isClimbing) { isClimbing = true; rb.gravityScale = 0f; }
            if (isClimbing)
            {
                float mIn2 = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  mIn2 = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) mIn2 =  1f;
                if (vIn > 0) IgnoreCeilings(); else RestoreCeilings();
                rb.linearVelocity = new Vector2(mIn2 * speed, vIn * climbSpeed);
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RestoreCeilings(); isOnLadder = false; isClimbing = false;
                    rb.gravityScale   = originalGravityScale;
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    lastJumpTime = Time.time;
                    jumpsLeft = Mathf.Max(0, maxJumps - 1); jumpBufferTimer = 0;
                }
                if (anim != null) anim.SetFloat("MoveSpeed", Mathf.Abs(vIn));
                return;
            }
        }
        else if (isClimbing) { isClimbing = false; rb.gravityScale = originalGravityScale; }

        float moveInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  moveInput = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput =  1f;

        float effectiveSpeed = speed * (rageSystem != null ? rageSystem.SpeedMultiplier : 1f);
        rb.linearVelocity = new Vector2(moveInput * effectiveSpeed, rb.linearVelocity.y);

        if (anim != null) anim.SetFloat("MoveSpeed", Mathf.Abs(moveInput));
        if (attackCooldownTimer <= 0f)
        {
            if (moveInput > 0) transform.localScale = new Vector3(-baseScaleX, baseScaleY, baseScaleZ);
            if (moveInput < 0) transform.localScale = new Vector3( baseScaleX, baseScaleY, baseScaleZ);
        }

        bool canJump = coyoteTimer > 0 || jumpsLeft > 0;
        if (jumpBufferTimer > 0 && canJump)
        {
            if (isWallSliding) { rb.linearVelocity = new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY); jumpsLeft = maxJumps - 1; }
            else               { rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); jumpsLeft = Mathf.Max(0, jumpsLeft - 1); coyoteTimer = 0; }
            lastJumpTime = Time.time;
            jumpBufferTimer = 0;
        }
    }

    void FinishPlunge()
    {
        isPlunging          = false;
        plungeGraceTimer    = 0.55f;
        plungeAnimTriggered = false;
        BanditAI        target           = plungeTarget;
        HeavyBanditBoss bossTarget       = plungeTargetBoss;
        BossHealth      bossHealthTarget = plungeTargetBossHealth;
        plungeTarget           = null;
        plungeTargetBoss       = null;
        plungeTargetBossHealth = null;
        ShowPlungePrompt(false);

        if (rb.gravityScale == 0f) rb.gravityScale = originalGravityScale;

        if (target != null)
        {
                ActionSnapshot snap = GetCurrentSnapshot();
                BanditMemory.RecordPlayerAttack(PlayerAttackType.Plunge, snap);
                target.TakeDamageFromPlayer(plungeDamage, PlayerAttackType.Plunge, snap, this);
                target.StunWithKnockback(0.5f, new Vector2(0f, -2f));

            if (playerCol != null)
                foreach (var ec in target.GetComponentsInChildren<Collider2D>(true))
                {
                    Physics2D.IgnoreCollision(playerCol, ec, true);
                    StartCoroutine(RestoreCollision(playerCol, ec, 1.5f));
                }
        }
        else if (bossTarget != null)
        {
            bossTarget.TakeDamage(plungeDamage);
            if (playerCol != null)
                foreach (var ec in bossTarget.GetComponentsInChildren<Collider2D>(true))
                {
                    Physics2D.IgnoreCollision(playerCol, ec, true);
                    StartCoroutine(RestoreCollision(playerCol, ec, 1.5f));
                }
        }
        else if (bossHealthTarget != null)
        {
            bossHealthTarget.TakeDamage(plungeDamage);
            if (playerCol != null)
                foreach (var ec in bossHealthTarget.GetComponentsInChildren<Collider2D>(true))
                {
                    Physics2D.IgnoreCollision(playerCol, ec, true);
                    StartCoroutine(RestoreCollision(playerCol, ec, 1.5f));
                }
        }

        float bounceX = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  bounceX = -plungeBounceX;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) bounceX =  plungeBounceX;
        rb.linearVelocity = new Vector2(bounceX, plungeBounceY);
    }

    void DashThroughCheck()
    {
        if (dashThroughUsed || playerCol == null) return;

        Vector2 center = (Vector2)transform.position + new Vector2(dashDirection * 0.8f, 0f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(1.2f, 0.9f), 0f);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            BanditAI        bandit = col.GetComponentInParent<BanditAI>();
            EnemyHealthBar  bar    = col.GetComponentInParent<EnemyHealthBar>();
            HeavyBanditBoss boss   = col.GetComponentInParent<HeavyBanditBoss>();
            BossHealth      bossH  = col.GetComponentInParent<BossHealth>();
            if (bandit == null && bar == null && boss == null && bossH == null) continue;

            dashThroughUsed = true;
            if (boss != null) boss.TakeDamage(dashDamage);
            else if (bossH != null) bossH.TakeDamage(dashDamage);
            else if (bandit != null)
            {
                ActionSnapshot snap = GetCurrentSnapshot();
                BanditMemory.RecordPlayerAttack(PlayerAttackType.Dash, snap);
                bandit.TakeDamageFromPlayer(dashDamage, PlayerAttackType.Dash, snap, this);
            }
            else if (bar != null) bar.TakeDamage(dashDamage);
            if (bandit != null) bandit.ApplySlow(1.5f);

            Collider2D[] enemyCols = col.transform.root.GetComponentsInChildren<Collider2D>();
            foreach (var ec in enemyCols)
            {
                Physics2D.IgnoreCollision(playerCol, ec, true);
                StartCoroutine(RestoreCollision(playerCol, ec, dashDuration + 0.35f));
            }
            break;
        }
    }

    System.Collections.IEnumerator RestoreCollision(Collider2D a, Collider2D b, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (a != null && b != null) Physics2D.IgnoreCollision(a, b, false);
    }

    void ShowPlungePrompt(bool show)
    {
        if (show && plungePromptGO == null)
        {
            plungePromptGO = new GameObject("PlungePrompt");
            var canvas = plungePromptGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            plungePromptGO.transform.localScale = Vector3.one * 0.01f;
            var rt = plungePromptGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(plungePromptGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text      = "[F]  Удар сверху";
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize  = 34;
            text.color     = new Color(1f, 0.9f, 0.1f);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            var shadow = textGO.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(2, -2);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        }
        else if (!show && plungePromptGO != null)
        {
            Destroy(plungePromptGO);
            plungePromptGO = null;
        }

        if (plungePromptGO != null)
        {
            plungePromptGO.transform.position = transform.position + Vector3.up * 3.2f;
            if (Camera.main != null)
                plungePromptGO.transform.rotation = Camera.main.transform.rotation;
        }
    }

    void PerformComboAttack()
    {
        string[] triggers = { "Attack1", "Attack2", "Attack3" };
        SafeSetTrigger(triggers[comboStep]);

        float dir  = transform.localScale.x < 0 ? 1f : -1f;
        int   step = Mathf.Clamp(comboStep, 0, comboDamage.Length - 1);
        float rX   = step < comboRangeX.Length  ? comboRangeX[step]  : attackRange;
        float rY   = step < comboRangeY.Length  ? comboRangeY[step]  : 1.4f;
        float oX   = step < comboOffsetX.Length ? comboOffsetX[step] : 0.8f;
        float oY   = step < comboOffsetY.Length ? comboOffsetY[step] : 0.1f;

        Vector2 hitCenter    = (Vector2)transform.position + new Vector2(dir * oX, oY);
        const float hardMaxReach = 3.5f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitCenter, new Vector2(rX, rY), 0f);

        float rageMult = rageSystem != null ? rageSystem.DamageMultiplier : 1f;
        int  damage  = Mathf.RoundToInt(comboDamage[Mathf.Clamp(comboStep, 0, comboDamage.Length - 1)] * rageMult);
        bool isFinal = (comboStep == 2);
        bool bossHit = false;

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            EnemyHealthBar  bar    = col.GetComponentInParent<EnemyHealthBar>();
            BanditAI        bandit = col.GetComponentInParent<BanditAI>();
            HeavyBanditBoss boss   = col.GetComponentInParent<HeavyBanditBoss>();
            BossHealth      bossH  = col.GetComponentInParent<BossHealth>();
            if (bar == null && bandit == null && boss == null && bossH == null) continue;

            Transform root = bandit != null ? bandit.transform
                           : boss   != null ? boss.transform
                           : bossH  != null ? bossH.transform
                           : bar.transform;
            float rootDist = Vector2.Distance((Vector2)transform.position, (Vector2)root.position);
            float maxDist  = (bossH != null) ? hardMaxReach + 3f : hardMaxReach;
            if (rootDist > maxDist) continue;

            if (boss != null) { boss.TakeDamage(damage); bossHit = true; }
            else if (bossH != null) { bossH.TakeDamage(damage); bossHit = true; }
            else if (bandit != null)
            {
                PlayerAttackType type = comboStep == 0 ? PlayerAttackType.Combo1
                                      : comboStep == 1 ? PlayerAttackType.Combo2
                                      : PlayerAttackType.Combo3;
                ActionSnapshot snap = GetCurrentSnapshot();
                BanditMemory.RecordPlayerAttack(type, snap);
                bandit.TakeDamageFromPlayer(damage, type, snap, this);
            }
            else if (bar != null) bar.TakeDamage(damage);

            if (bandit != null && isFinal)
                bandit.StunWithKnockback(0.65f, new Vector2(dir * 6f, 3f));
        }

        if (!bossHit)
        {
            if (cachedBoss == null) cachedBoss = FindFirstObjectByType<HeavyBanditBoss>();
            if (cachedBoss != null && !cachedBoss.IsDead)
            {
                Vector2 toBoss = (Vector2)cachedBoss.transform.position - (Vector2)transform.position;
                bool inFront   = Mathf.Sign(toBoss.x) == Mathf.Sign(dir) || Mathf.Abs(toBoss.x) < 0.5f;
                if (inFront && toBoss.magnitude <= hardMaxReach + 1.5f)
                    cachedBoss.TakeDamage(damage);
            }

            var brB = FindFirstObjectByType<BringerOfDeathBoss>();
            if (brB != null && !brB.IsDead)
            {
                Vector2 toBoss = (Vector2)brB.transform.position - (Vector2)transform.position;
                bool inFront   = Mathf.Sign(toBoss.x) == Mathf.Sign(dir) || Mathf.Abs(toBoss.x) < 0.5f;
                if (inFront && toBoss.magnitude <= hardMaxReach + 1.5f)
                    brB.TakeDamage(damage);
            }

            if (cachedBossHealth == null) cachedBossHealth = FindFirstObjectByType<BossHealth>();
            if (cachedBossHealth != null && !cachedBossHealth.IsDead())
            {
                Vector2 toBoss   = (Vector2)cachedBossHealth.transform.position - (Vector2)transform.position;
                float   hDistBH  = Mathf.Abs(toBoss.x);
                bool    inFront  = Mathf.Sign(toBoss.x) == Mathf.Sign(dir) || hDistBH < 0.5f;
                if (inFront && hDistBH <= hardMaxReach + 3f)
                    cachedBossHealth.TakeDamage(damage);
            }
        }

        bool wasFinal       = (comboStep == 2);
        comboStep           = wasFinal ? 0 : comboStep + 1;
        attackCooldownTimer = wasFinal ? 0.7f : attackCooldown;
        comboResetTimer     = wasFinal ? 0f   : comboWindow;
    }

    public void DoParry()
    {
        ParryActive        = false;
        parryTimer         = 0f;
        parryCooldownTimer = parryCooldown;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.5f);
        foreach (var col in hits)
        {
            BanditAI b = col.GetComponentInParent<BanditAI>()
                      ?? col.transform.root.GetComponentInChildren<BanditAI>();
            if (b != null) { b.Stun(parryStunDuration); b.TakeDamage(parryReflectDamage); }

            HeavyBanditBoss boss  = col.GetComponentInParent<HeavyBanditBoss>();
            if (boss  != null) boss.TakeDamage(parryReflectDamage);
            BossHealth  bossH = col.GetComponentInParent<BossHealth>();
            if (bossH != null) bossH.TakeDamage(parryReflectDamage);
        }
    }

    System.Collections.IEnumerator DashBlinkRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dashDuration + 0.05f)
        {
            if (playerSprite != null) playerSprite.enabled = !playerSprite.enabled;
            yield return new WaitForSeconds(0.04f);
            elapsed += 0.04f;
        }
        if (playerSprite != null) playerSprite.enabled = true;
        IsDashInvincible = false;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (IsDead || IsGrounded()) return;
        if (((1 << col.gameObject.layer) & groundLayer.value) != 0)
            jumpsLeft = maxJumps;
    }

    void SafeSetTrigger(string name, string fallback = "Attack")
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            { anim.SetTrigger(name); return; }
        foreach (var p in anim.parameters)
            if (p.name == fallback && p.type == AnimatorControllerParameterType.Trigger)
            { anim.SetTrigger(fallback); return; }
    }

    public ActionSnapshot GetCurrentSnapshot()
    {
        float moveX = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;

        return new ActionSnapshot
        {
            moveDir = (sbyte)Mathf.Sign(moveX),
            inAir = !IsGrounded(),
            recentDash = Time.time - lastDashTime < 0.5f,
            recentJump = Time.time - lastJumpTime < 0.5f,
            wasBlocking = IsBlocking
        };
    }
}
