using UnityEngine;
using System.Collections;

public class HeavyBanditBoss : MonoBehaviour
{
    [Header("Базовые параметры")]
    public int   maxHP          = 200;
    public float speed          = 3.5f;
    public float chaseSpeedMult = 1.4f;

    [Header("Комбо-атака")]
    public int   comboDamage1         = 12;
    public int   comboDamage2         = 14;
    public int   comboDamage3         = 18;
    public float comboInterval        = 0.3f;
    public float comboRange           = 2.2f;
    public float comboStartRange      = 5.5f;
    public float comboSightRange      = 8f;
    public float vulnerableAfterCombo = 0.8f;

    [Header("Теневой рывок")]
    public int   shadowDashDamage    = 20;
    public float shadowBlinkDuration = 0.5f;
    public float shadowCounterWindow = 0.3f;
    public int   shadowCounterMult   = 2;

    [Header("Бросок ножей")]
    public GameObject knifePrefab;
    public float      knifeSpeed  = 12f;
    public int        knifeDamage = 8;

    [Header("Границы арены")]
    public float arenaMinX = -20f;
    public float arenaMaxX =  20f;

    [Header("Телепорт за спину")]
    public float teleportBehindGap = 1.1f;

    [Header("Переход в фазу 2")]
    public GameObject phase2Prefab;
    public float      ascendHeight  = 6f;
    public float      ascendSpeed   = 4f;
    public float      recoverDelay  = 1.2f;
    public bool       useRecoverAnim = true;

    enum BossState { Idle, Chase, Combo, ShadowDash, KnifeFan, Vulnerable, Dead }
    BossState state = BossState.Idle;

    int   currentHP;
    float baseScaleX, baseScaleY, baseScaleZ;

    Rigidbody2D    rb;
    Animator       anim;
    Transform      player;
    PlayerHealth   playerHealth;
    BossHPBar      hpBar;
    SpriteRenderer sr;

    bool  isActive;
    bool  isVulnerable;
    bool  shadowCounterActive;
    bool  wasCounterHit;
    int   hitsReceived;
    float hitResetTimer;

    void Start()
    {
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>(true);
        sr = GetComponentInChildren<SpriteRenderer>();

        currentHP  = maxHP;
        baseScaleX = Mathf.Abs(transform.localScale.x);
        baseScaleY = transform.localScale.y;
        baseScaleZ = transform.localScale.z;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            player       = pc.transform;
            playerHealth = pc.GetComponent<PlayerHealth>()
                        ?? pc.GetComponentInParent<PlayerHealth>()
                        ?? FindFirstObjectByType<PlayerHealth>();
        }
        hpBar = FindFirstObjectByType<BossHPBar>();
    }

    void Update()
    {
        if (!isActive || state == BossState.Dead) return;
        if (hitResetTimer > 0f) { hitResetTimer -= Time.deltaTime; if (hitResetTimer <= 0f) hitsReceived = 0; }
    }

    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        if (hpBar != null) hpBar.SetVisible(true);
        StartCoroutine(BossLoop());
    }

    bool IsEnraged => currentHP <= maxHP / 2;

    IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(0.6f);
        float nextKnife  = Time.time + Random.Range(6f, 9f);
        float nextDash   = Time.time + 4f;
        int   chaseCount = 0;

        while (state != BossState.Dead)
        {
            if (player == null) { yield return null; continue; }

            float hdist     = Mathf.Abs(GetVisualCenter(transform).x - GetVisualCenter(player).x);
            bool  enraged   = IsEnraged;
            bool  canDash   = Time.time >= nextDash;
            float dashCD    = enraged ? 4f : 6f;

            if (hitsReceived >= (enraged ? 2 : 3) && canDash)
            {
                nextDash   = Time.time + dashCD;
                chaseCount = 0;
                yield return StartCoroutine(DoShadowDash());
            }
            else if (Time.time >= nextKnife)
            {
                nextKnife  = Time.time + Random.Range(enraged ? 4f : 6f, enraged ? 7f : 10f);
                chaseCount = 0;
                yield return StartCoroutine(DoKnifeFan());
            }
            else if (hdist <= comboStartRange)
            {
                chaseCount = 0;
                yield return StartCoroutine(DoCombo());
            }
            else if (hdist <= comboSightRange)
            {
                chaseCount = 0;
                yield return StartCoroutine(DoChase(0.4f));
                if (Mathf.Abs(GetVisualCenter(transform).x - GetVisualCenter(player).x) <= comboStartRange)
                    yield return StartCoroutine(DoCombo());
            }
            else if (canDash)
            {
                nextDash   = Time.time + dashCD;
                chaseCount = 0;
                yield return StartCoroutine(DoShadowDash());
            }
            else
            {
                chaseCount++;
                yield return StartCoroutine(DoChase(enraged ? 0.9f : 1.2f));
            }

            yield return new WaitForSeconds(enraged ? 0.08f : 0.15f);
        }
    }

    IEnumerator DoChase(float dur)
    {
        state = BossState.Chase;
        for (float t = 0f; t < dur && state == BossState.Chase; t += Time.deltaTime)
        {
            if (player != null)
            {
                int d = player.position.x > transform.position.x ? 1 : -1;
                rb.linearVelocity = new Vector2(speed * chaseSpeedMult * d, rb.linearVelocity.y);
                FacePlayer();
            }
            yield return null;
        }
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        state = BossState.Idle;
    }

    IEnumerator DoCombo()
    {
        state = BossState.Combo;
        FacePlayer();

        if (player != null)
        {
            Vector2 bossV0 = GetVisualCenter(transform);
            Vector2 plyV0  = GetVisualCenter(player);
            float distance     = Mathf.Abs(plyV0.x - bossV0.x);
            float dashDir      = plyV0.x > bossV0.x ? 1f : -1f;
            float dashDuration = Mathf.Clamp(distance / (speed * 4f), 0.12f, 0.4f);
            float dashSpeed    = Mathf.Clamp(distance / dashDuration, speed * 2.5f, speed * 8f);
            rb.linearVelocity  = new Vector2(dashDir * dashSpeed, rb.linearVelocity.y);
            yield return new WaitForSeconds(dashDuration);
            rb.linearVelocity  = new Vector2(0f, rb.linearVelocity.y);

            if (player != null)
            {
                Vector2 bV = GetVisualCenter(transform);
                Vector2 pV = GetVisualCenter(player);
                Collider2D bColC = GetPrimaryCollider(transform);
                float bossHalfWC = bColC != null ? bColC.bounds.extents.x : 0.5f;
                float edgeDistC  = Mathf.Max(0f, Mathf.Abs(bV.x - pV.x) - bossHalfWC);
                if (edgeDistC > meleeHitRange - 0.3f)
                {
                    float side  = bV.x < pV.x ? -1f : 1f;
                    float bOff  = bV.x - transform.position.x;
                    float targetCenterX = pV.x + side * (bossHalfWC + meleeHitRange - 0.5f);
                    float snapX = targetCenterX - bOff;
                    transform.position = new Vector3(snapX, transform.position.y, transform.position.z);
                }
            }
        }
        else
        {
            yield return new WaitForSeconds(0.18f);
        }

        lockFacing = true;

        float interval  = IsEnraged ? comboInterval * 0.55f    : comboInterval;
        float vulnTime  = IsEnraged ? vulnerableAfterCombo * 0.5f : vulnerableAfterCombo;

        yield return StartCoroutine(LungeAtPlayer(0.1f));
        ResetAnimTrigger("Attack");
        TriggerAnim("Attack"); FlashColor(new Color(1f, 0.4f, 0.4f));
        yield return new WaitForSeconds(0.12f);
        TryHit(comboDamage1, 0f, 0f);
        yield return new WaitForSeconds(interval);
        if (state == BossState.Dead) yield break;

        yield return StartCoroutine(LungeAtPlayer(0.1f));
        ResetAnimTrigger("Attack");
        TriggerAnim("Attack"); FlashColor(new Color(1f, 0.4f, 0.4f));
        yield return new WaitForSeconds(0.12f);
        TryHit(comboDamage2, 0f, 0f);
        yield return new WaitForSeconds(interval);
        if (state == BossState.Dead) yield break;

        yield return StartCoroutine(LungeAtPlayer(0.12f));
        ResetAnimTrigger("Attack");
        TriggerAnim("Attack");

        for (int i = 0; i < 7; i++)
        {
            if (sr != null) sr.color = (i % 2 == 0) ? new Color(1f, 0.1f, 0.1f) : Color.white;
            yield return new WaitForSeconds(0.06f);
        }
        if (sr != null) sr.color = Color.white;

        if (state == BossState.Dead) yield break;
        float kbDir = player != null && player.position.x > transform.position.x ? 1f : -1f;
        TryHit(comboDamage3, kbDir * 10f, 4f);
        yield return new WaitForSeconds(0.15f);

        lockFacing   = false;
        state        = BossState.Vulnerable;
        isVulnerable = true;
        TriggerAnim("Hurt");
        yield return new WaitForSeconds(vulnTime);
        isVulnerable = false;
        hitsReceived = 0;
        state        = BossState.Idle;
    }

    IEnumerator DoShadowDash()
    {
        state = BossState.ShadowDash;
        rb.linearVelocity = Vector2.zero;
        hitsReceived = 0;

        if (sr != null)
        {
            for (float t = 0f; t < shadowBlinkDuration; t += Time.deltaTime)
            { sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.1f, t / shadowBlinkDuration)); yield return null; }
            sr.color = new Color(1f, 1f, 1f, 0f);
        }
        else yield return new WaitForSeconds(shadowBlinkDuration);

        if (state == BossState.Dead) yield break;

        Transform pT = player;
        if (pT == null)
        {
            var pgo = GameObject.FindWithTag("Player");
            if (pgo != null) { pT = pgo.transform; player = pT; }
        }
        if (pT == null)
        {
            var anyPc = FindFirstObjectByType<PlayerController>();
            if (anyPc != null) { pT = anyPc.transform; player = pT; }
        }

        if (pT != null)
        {
            Vector2 playerCenter = GetVisualCenter(pT);
            Vector2 bossCenter   = GetVisualCenter(transform);
            Vector2 bossOffset   = bossCenter - (Vector2)transform.position;

            bool facingLeft = pT.localScale.x > 0f;
            float behindDir = facingLeft ? 1f : -1f;

            Collider2D bCol = GetPrimaryCollider(transform);
            Collider2D pCol = GetPrimaryCollider(pT);
            float bossHalfW   = bCol != null ? bCol.bounds.extents.x : 0.5f;
            float playerHalfW = pCol != null ? pCol.bounds.extents.x : 0.5f;
            float behindDistance = bossHalfW + playerHalfW + teleportBehindGap;

            float desiredCenterX = playerCenter.x + behindDir * behindDistance;
            float teleportX      = desiredCenterX - bossOffset.x;

            float teleportY = transform.position.y;
            if (bCol != null && pCol != null)
            {
                float yShift = pCol.bounds.min.y - bCol.bounds.min.y;
                teleportY = transform.position.y + yShift;
            }

            transform.position = new Vector3(teleportX, teleportY, transform.position.z);
            rb.linearVelocity  = Vector2.zero;

            Debug.Log($"[BOSS TELEPORT] player.x={pT.position.x:F2} playerCenter.x={playerCenter.x:F2} bossHalfW={bossHalfW:F2} playerHalfW={playerHalfW:F2} dist={behindDistance:F2} facingLeft={facingLeft} -> boss.transform.x={teleportX:F2}");
        }
        else
        {
            Debug.LogWarning("[BOSS TELEPORT] player reference is NULL — boss can't teleport behind player!");
        }
        if (sr != null) sr.color = Color.white;
        FacePlayer();

        wasCounterHit       = false;
        shadowCounterActive = true;
        for (float t = 0f; t < shadowCounterWindow; t += Time.deltaTime)
        { if (wasCounterHit) break; yield return null; }
        shadowCounterActive = false;

        if (state == BossState.Dead) yield break;

        if (wasCounterHit)
        {
            state        = BossState.Vulnerable;
            isVulnerable = true;
            TriggerAnim("Hurt");
            yield return new WaitForSeconds(0.6f);
            isVulnerable = false;
        }
        else
        {
            TriggerAnim("Attack");
            float atkDir = player != null && player.position.x > transform.position.x ? 1f : -1f;
            TryHit(shadowDashDamage, atkDir * 8f, 3f);
            yield return new WaitForSeconds(0.3f);
        }
        state = BossState.Idle;
    }

    IEnumerator DoKnifeFan()
    {
        state        = BossState.KnifeFan;
        isVulnerable = true;
        float flipDir = player != null && player.position.x > transform.position.x ? -1f : 1f;
        rb.linearVelocity = new Vector2(flipDir * speed * 2f, 7f);
        yield return new WaitForSeconds(0.45f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.3f);
        isVulnerable = false;

        if (state == BossState.Dead || player == null) { state = BossState.Idle; yield break; }

        float[] offsets  = { -30f, -15f, 0f, 15f, 30f };
        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float   baseAng  = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        foreach (float off in offsets)
        {
            float   ang     = (baseAng + off) * Mathf.Deg2Rad;
            Vector2 dir     = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector3 spawnAt = transform.position + Vector3.up * 0.5f;
            Quaternion rot  = Quaternion.Euler(0f, 0f, baseAng + off);

            GameObject knife = knifePrefab != null
                ? Instantiate(knifePrefab, spawnAt, rot)
                : CreateGreyRectProjectile(spawnAt, rot);

            var kRb = knife.GetComponent<Rigidbody2D>();
            if (kRb != null) { kRb.gravityScale = 0.2f; kRb.linearVelocity = dir * knifeSpeed; }
            var kp = knife.GetComponent<KnifeProjectile>();
            if (kp != null) kp.damage = knifeDamage;
            yield return new WaitForSeconds(0.06f);
        }
        yield return new WaitForSeconds(0.5f);
        state = BossState.Idle;
    }

    IEnumerator LungeAtPlayer(float dur)
    {
        if (player == null) yield break;
        Vector2 bV0 = GetVisualCenter(transform);
        Vector2 pV0 = GetVisualCenter(player);
        float d          = pV0.x > bV0.x ? 1f : -1f;
        float distance   = Mathf.Abs(pV0.x - bV0.x);
        float lungeSpeed = Mathf.Clamp(distance / dur, speed * 2f, speed * 6f);
        rb.linearVelocity = new Vector2(d * lungeSpeed, rb.linearVelocity.y);
        yield return new WaitForSeconds(dur);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (player != null)
        {
            Vector2 bV = GetVisualCenter(transform);
            Vector2 pV = GetVisualCenter(player);
            Collider2D bCol = GetPrimaryCollider(transform);
            float bossHalfW = bCol != null ? bCol.bounds.extents.x : 0.5f;
            float edgeDist  = Mathf.Max(0f, Mathf.Abs(bV.x - pV.x) - bossHalfW);
            if (edgeDist > meleeHitRange - 0.3f)
            {
                float side  = bV.x < pV.x ? -1f : 1f;
                float bOff  = bV.x - transform.position.x;
                float targetCenterX = pV.x + side * (bossHalfW + meleeHitRange - 0.5f);
                float snapX = targetCenterX - bOff;
                transform.position = new Vector3(snapX, transform.position.y, transform.position.z);
            }
        }
    }

    void FlashColor(Color c)
    {
        if (sr == null) return;
        StopCoroutine(nameof(FlashColorRoutine));
        StartCoroutine(FlashColorRoutine(c));
    }

    IEnumerator FlashColorRoutine(Color c)
    {
        if (sr == null) yield break;
        Color orig = Color.white;
        Vector3 baseS = new Vector3(baseScaleX, baseScaleY, baseScaleZ);
        float sign = Mathf.Sign(transform.localScale.x);

        sr.color = c;
        transform.localScale = new Vector3(sign * baseScaleX * 1.18f, baseScaleY * 1.18f, baseScaleZ);
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = orig;
        transform.localScale = new Vector3(sign * baseScaleX, baseScaleY, baseScaleZ);
    }

    GameObject CreateGreyRectProjectile(Vector3 pos, Quaternion rot)
    {
        var go = new GameObject("GreyProjectile");
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = new Vector3(0.5f, 0.18f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeWhiteSprite();
        sr.color  = new Color(0.55f, 0.55f, 0.55f);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        go.AddComponent<Rigidbody2D>();
        go.AddComponent<KnifeProjectile>();
        return go;
    }

    static Sprite cachedWhiteSprite;
    static Sprite MakeWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;
        var tex = Texture2D.whiteTexture;
        cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
        return cachedWhiteSprite;
    }

    public float meleeHitRange = 2.0f;

    void TryHit(int dmg, float kbX, float kbY)
    {
        if (player == null || playerHealth == null) return;

        Vector2 bossC   = GetVisualCenter(transform);
        Vector2 playerC = GetVisualCenter(player);
        Collider2D bCol = GetPrimaryCollider(transform);
        Collider2D pCol = GetPrimaryCollider(player);

        float bossHalfW   = bCol != null ? bCol.bounds.extents.x : 0.5f;
        float edgeDistX   = Mathf.Max(0f, Mathf.Abs(bossC.x - playerC.x) - bossHalfW);
        if (edgeDistX > meleeHitRange) return;

        if (bCol != null && pCol != null)
        {
            if (Mathf.Abs(bCol.bounds.min.y - pCol.bounds.min.y) > 1.5f) return;
        }
        else if (Mathf.Abs(bossC.y - playerC.y) > 1.8f) return;

        if (kbX != 0f || kbY != 0f)
        {
            var pRb = player.GetComponent<Rigidbody2D>() ?? player.GetComponentInParent<Rigidbody2D>();
            if (pRb != null) { pRb.linearVelocity = Vector2.zero; pRb.AddForce(new Vector2(kbX, kbY), ForceMode2D.Impulse); }
        }
        playerHealth.TakeDamage(dmg);
    }

    public void TakeDamage(int amount)
    {
        if (state == BossState.Dead) return;

        int actual = amount;
        if (shadowCounterActive)
        {
            actual              = amount * shadowCounterMult;
            wasCounterHit       = true;
            shadowCounterActive = false;
        }

        hitsReceived++;
        hitResetTimer = 2.5f;
        currentHP = Mathf.Max(0, currentHP - actual);

        BloodParticles.Spawn(transform.position + Vector3.up * 0.6f, actual, transform);
        DamageNumber.Show(actual, transform.position + Vector3.up * 1.3f);
        if (hpBar != null) hpBar.UpdateHP((float)currentHP / maxHP);

        if (currentHP <= 0)
        {
            state = BossState.Dead;
            StopAllCoroutines();
            StartCoroutine(DieRoutine());
            return;
        }

        if (state != BossState.ShadowDash) TriggerAnim("Hurt");
    }

    IEnumerator DieRoutine()
    {
        Vector3 deathPos = transform.position;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Kinematic;
        if (sr != null) sr.color = Color.white;
        TriggerAnim("Die");
        if (hpBar != null) hpBar.SetVisible(false);
        yield return new WaitForSeconds(1.5f);

        if (phase2Prefab == null) { Destroy(gameObject); yield break; }

        ResetAnimTrigger("Die");
        TriggerAnim("Recover");
        FlashColor(new Color(0.5f, 0.8f, 1f));
        yield return new WaitForSeconds(recoverDelay);

        ResetAnimTrigger("Recover");
        TriggerAnim("Idle");
        yield return new WaitForSeconds(0.4f);

        Vector3 startPos  = transform.position;
        Vector3 targetPos = startPos + Vector3.up * ascendHeight;
        float   t         = 0f;
        float   duration  = ascendHeight / ascendSpeed;
        while (t < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, t / duration);
            FlashColor(new Color(0.5f, 0.8f, 1f, 0.7f));
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        Vector3 spawnPos = new Vector3(deathPos.x, deathPos.y + 0.5f, deathPos.z);
        var phase2 = Instantiate(phase2Prefab, spawnPos, Quaternion.identity);
        Destroy(gameObject);
    }

    bool lockFacing;

    void FacePlayer()
    {
        if (player == null || lockFacing) return;
        float d = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(-d * baseScaleX, baseScaleY, baseScaleZ);
    }

    static Vector2 GetEntityCenter(Transform t)
    {
        var rootCol = t.GetComponent<Collider2D>();
        if (rootCol != null && !rootCol.isTrigger) return rootCol.bounds.center;
        foreach (var c in t.GetComponentsInChildren<Collider2D>())
            if (c != null && !c.isTrigger) return c.bounds.center;
        var sr2 = t.GetComponent<SpriteRenderer>() ?? t.GetComponentInChildren<SpriteRenderer>();
        if (sr2 != null) return sr2.bounds.center;
        return t.position;
    }

    static Collider2D GetPrimaryCollider(Transform t)
    {
        var rootCol = t.GetComponent<Collider2D>();
        if (rootCol != null && !rootCol.isTrigger) return rootCol;
        foreach (var c in t.GetComponentsInChildren<Collider2D>())
            if (c != null && !c.isTrigger) return c;
        return null;
    }

    static Vector2 GetVisualCenter(Transform t)
    {
        var rootCol = t.GetComponent<Collider2D>();
        if (rootCol != null && !rootCol.isTrigger) return rootCol.bounds.center;
        foreach (var c in t.GetComponentsInChildren<Collider2D>())
            if (c != null && !c.isTrigger) return c.bounds.center;
        var sr2 = t.GetComponent<SpriteRenderer>() ?? t.GetComponentInChildren<SpriteRenderer>();
        if (sr2 != null && sr2.sprite != null) return sr2.bounds.center;
        return t.position;
    }

    void TriggerAnim(string name)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            { anim.SetTrigger(name); return; }
    }

    void ResetAnimTrigger(string name)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            { anim.ResetTrigger(name); return; }
    }

    public bool IsDead       => state == BossState.Dead;
    public bool IsVulnerable => isVulnerable;

    public int HookReaction()
    {
        if (state == BossState.Dead) return 0;
        if (currentHP > maxHP * 0.5f) return 2;
        float chance = currentHP <= maxHP * 0.3f ? 0.2f : 0.6f;
        if (Random.value <= chance) return 1;
        return 0;
    }

    public void ExecuteGrabHook()
    {
        StopAllCoroutines();
        StartCoroutine(GrabHookRoutine());
    }

    public void ExecuteDodge()
    {
        StopAllCoroutines();
        StartCoroutine(DodgeBackflipRoutine());
    }

    IEnumerator GrabHookRoutine()
    {
        state = BossState.Combo;
        FacePlayer();
        lockFacing = true;
        FlashColor(new Color(1f, 0.6f, 0.1f));

        var pc  = player != null ? player.GetComponent<PlayerController>() : null;
        var pRb = player != null ? (player.GetComponent<Rigidbody2D>() ?? player.GetComponentInParent<Rigidbody2D>()) : null;
        var ph  = playerHealth;

        if (pc != null) pc.enabled = false;

        RigidbodyType2D origType = RigidbodyType2D.Dynamic;
        float           origGrav = 3f;
        if (pRb != null)
        {
            origType            = pRb.bodyType;
            origGrav            = pRb.gravityScale;
            pRb.linearVelocity  = Vector2.zero;
            pRb.gravityScale    = 0f;
            pRb.bodyType        = RigidbodyType2D.Kinematic;
        }

        if (player != null && pRb != null)
        {
            float pullDuration = 0.4f;
            float t = 0f;
            Vector2 startPos = pRb.position;
            float   side     = startPos.x > transform.position.x ? 1f : -1f;
            Vector2 endPos   = (Vector2)transform.position + new Vector2(side * 1.2f, 0f);

            while (t < pullDuration && player != null)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / pullDuration);
                Vector2 newPlayerPos = Vector2.Lerp(startPos, endPos, k);
                pRb.position       = newPlayerPos;
                pRb.linearVelocity = Vector2.zero;
                t += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.05f);

        ResetAnimTrigger("Attack");
        TriggerAnim("Attack");
        FlashColor(new Color(1f, 0.15f, 0.15f));
        yield return new WaitForSeconds(0.18f);

        if (pRb != null)
        {
            pRb.bodyType     = origType;
            pRb.gravityScale = origGrav;
        }

        if (state != BossState.Dead && player != null && ph != null)
        {
            float kbDir = player.position.x > transform.position.x ? 1f : -1f;
            int   dmg   = comboDamage3 * 2;
            if (pRb != null) { pRb.linearVelocity = Vector2.zero; pRb.AddForce(new Vector2(kbDir * 12f, 5f), ForceMode2D.Impulse); }
            ph.TakeDamage(dmg);
        }

        if (pc != null) pc.enabled = true;

        yield return new WaitForSeconds(0.3f);
        lockFacing = false;
        state = BossState.Idle;
        StartCoroutine(BossLoop());
    }

    IEnumerator DodgeBackflipRoutine()
    {
        state = BossState.Idle;
        FacePlayer();
        float dir = player != null && player.position.x > transform.position.x ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * speed * 2.5f, 8f);
        TriggerAnim("Hurt");
        yield return new WaitForSeconds(0.5f);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(BossLoop());
    }
}
