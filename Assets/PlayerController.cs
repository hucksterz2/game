using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float speed = 5f;

    [Header("Прыжок")]
    public float jumpForce = 10f;
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
    public float attackRange    = 1.4f;
    public float attackCooldown = 0.25f;
    public float comboWindow    = 0.8f;
    public int[] comboDamage    = { 8, 13, 20 };

    [Header("Блок / Парирование (Q)")]
    public float blockDamageReduction = 0.5f;
    public float parryWindow          = 0.2f;
    public float parryStunDuration    = 1.5f;
    public float parryCooldown        = 0.8f;

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

    private bool isTouchingWall, isWallSliding;
    private int  wallDirection;

    private bool       isPlunging;
    private bool       plungeAnimTriggered;
    private float      plungeStartDist;
    private float      plungeTimer;
    private BanditAI   plungeTarget;
    private GameObject plungePromptGO;

    private RageSystem rageSystem;
    private SpriteRenderer playerSprite;

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

        rageSystem   = GetComponent<RageSystem>() ?? GetComponentInParent<RageSystem>();
        playerSprite = GetComponentInChildren<SpriteRenderer>();
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
        if (IsDead) return;
        CheckLadder();

        attackCooldownTimer -= Time.deltaTime;
        if (comboResetTimer > 0f)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0f) comboStep = 0;
        }
        if (Keyboard.current.fKey.wasPressedThisFrame && attackCooldownTimer <= 0f && !isDashing && !isPlunging)
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

        if (grounded) { coyoteTimer = coyoteTime; if (!wasGrounded) jumpsLeft = maxJumps; }
        else          coyoteTimer -= Time.deltaTime;
        wasGrounded = grounded;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        isTouchingWall = IsTouchingWall(out wallDirection);
        isWallSliding  = isTouchingWall && !grounded && rb.linearVelocity.y < 0;
        if (isWallSliding) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

        dashCooldownTimer -= Time.deltaTime;
        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && dashCooldownTimer <= 0 && !isDashing && !isPlunging)
        {
            float move = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  move = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move =  1f;
            if (move == 0f) move = transform.localScale.x > 0 ? -1f : 1f;
            dashDirection = move; isDashing = true; dashTimer = dashDuration;
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
            if (plungeTarget == null) { isPlunging = false; plungeAnimTriggered = false; ShowPlungePrompt(false); }
            else
            {
                plungeTimer += Time.deltaTime;
                float dist = Vector2.Distance(transform.position, plungeTarget.transform.position);

                float curPlungeSpeed = dist < 2.2f ? plungeSpeed * 0.35f : plungeSpeed;
                rb.linearVelocity = new Vector2(0f, -curPlungeSpeed);

                if (!plungeAnimTriggered && (dist < plungeStartDist * 0.55f || plungeTimer > 0.18f))
                {
                    plungeAnimTriggered = true;
                    SafeSetTrigger("Plunge");
                }

                if (plungeTimer > 0.1f && (dist < 0.75f || grounded)) FinishPlunge();
            }
            return;
        }

        if (GrapplingHook.isSwinging)
        {
            if (rb.linearVelocity.x > 0.2f)       transform.localScale = new Vector3(-baseScaleX, baseScaleY, baseScaleZ);
            else if (rb.linearVelocity.x < -0.2f)  transform.localScale = new Vector3( baseScaleX, baseScaleY, baseScaleZ);
            if (anim != null) anim.SetFloat("MoveSpeed", 1f);
            return;
        }

        if (!grounded && rb.linearVelocity.y < -1.5f && plungeTarget == null)
        {
            Vector2 boxCenter = (Vector2)transform.position + Vector2.down * (plungeDetectDist * 0.5f + 0.5f);
            Collider2D[] belowHits = Physics2D.OverlapBoxAll(boxCenter, new Vector2(0.8f, plungeDetectDist), 0f);
            foreach (var col in belowHits)
            {
                BanditAI b = col.GetComponentInParent<BanditAI>();
                if (b != null) { plungeTarget = b; break; }
            }
        }
        else if (grounded || rb.linearVelocity.y >= 0f)
        {
            if (plungeTarget != null) { plungeTarget = null; ShowPlungePrompt(false); }
        }

        ShowPlungePrompt(plungeTarget != null);

        if (plungeTarget != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            isPlunging          = true;
            plungeAnimTriggered = false;
            plungeTimer         = 0f;
            plungeStartDist = Vector2.Distance(transform.position, plungeTarget.transform.position);
            if (plungeStartDist < 0.1f) plungeStartDist = 3f;
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
        if (moveInput > 0) transform.localScale = new Vector3(-baseScaleX, baseScaleY, baseScaleZ);
        if (moveInput < 0) transform.localScale = new Vector3( baseScaleX, baseScaleY, baseScaleZ);

        bool canJump = coyoteTimer > 0 || jumpsLeft > 0;
        if (jumpBufferTimer > 0 && canJump)
        {
            if (isWallSliding) { rb.linearVelocity = new Vector2(-wallDirection * wallJumpForceX, wallJumpForceY); jumpsLeft = maxJumps - 1; }
            else               { rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); jumpsLeft = Mathf.Max(0, jumpsLeft - 1); coyoteTimer = 0; }
            jumpBufferTimer = 0;
        }
    }

    void FinishPlunge()
    {
        isPlunging          = false;
        plungeAnimTriggered = false;
        BanditAI target = plungeTarget;
        plungeTarget = null;
        ShowPlungePrompt(false);

        if (target != null)
        {
            EnemyHealthBar bar = target.GetComponent<EnemyHealthBar>();
            if (bar != null) bar.TakeDamage(plungeDamage);
            else             target.TakeDamage(plungeDamage);
            target.StunWithKnockback(0.5f, new Vector2(0f, -2f));
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
            BanditAI       bandit = col.GetComponentInParent<BanditAI>();
            EnemyHealthBar bar    = col.GetComponentInParent<EnemyHealthBar>();
            if (bandit == null && bar == null) continue;

            dashThroughUsed = true;
            if (bar    != null) bar.TakeDamage(dashDamage);
            else if (bandit != null) bandit.TakeDamage(dashDamage);
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
            rt.sizeDelta = new Vector2(120f, 26f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(plungePromptGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text      = "[E]  Удар сверху";
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize  = 20;
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
            plungePromptGO.transform.position = transform.position + Vector3.up * 1.9f;
            if (Camera.main != null)
                plungePromptGO.transform.rotation = Camera.main.transform.rotation;
        }
    }

    void PerformComboAttack()
    {
        string[] triggers = { "Attack1", "Attack2", "Attack3" };
        SafeSetTrigger(triggers[comboStep]);

        float dir = transform.localScale.x < 0 ? 1f : -1f;
        float clampedRange = Mathf.Clamp(attackRange, 0.3f, 2.5f);
        Vector2 hitCenter  = (Vector2)transform.position + new Vector2(dir * 0.8f, 0.1f);
        const float hardMaxReach = 2.8f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitCenter, new Vector2(clampedRange, 1.4f), 0f);

        float rageMult = rageSystem != null ? rageSystem.DamageMultiplier : 1f;
        int  damage  = Mathf.RoundToInt(comboDamage[Mathf.Clamp(comboStep, 0, comboDamage.Length - 1)] * rageMult);
        bool isFinal = (comboStep == 2);

        foreach (var col in hits)
        {
            if (col.gameObject == gameObject) continue;
            EnemyHealthBar bar    = col.GetComponentInParent<EnemyHealthBar>();
            BanditAI        bandit = col.GetComponentInParent<BanditAI>();
            if (bar == null && bandit == null) continue;

            Transform root = bandit != null ? bandit.transform : bar.transform;
            if (Vector2.Distance((Vector2)transform.position, (Vector2)root.position) > hardMaxReach)
                continue;

            if (bar != null)         bar.TakeDamage(damage);
            else if (bandit != null) bandit.TakeDamage(damage);

            if (bandit != null && isFinal)
                bandit.StunWithKnockback(0.65f, new Vector2(dir * 6f, 3f));
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
            BanditAI b = col.GetComponentInParent<BanditAI>();
            if (b != null) b.Stun(parryStunDuration);
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
}
