using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GrapplingHook : MonoBehaviour
{
    [Header("Настройки крюка")]
    public float maxDistance = 10f;
    public LayerMask grappleLayer;

    [Header("Подтягивание (верёвка)")]
    public float pullSpeed = 5f;
    public float minRopeLength = 1f;
    public float maxRopeLength = 15f;

    [Header("Раскачивание")]
    public float swingForce      = 14f;
    public float maxSwingSpeed   = 9f;
    public float minSpeedToClimb = 3.5f;

    [Header("Крюк к врагу")]
    public float enemyMassThreshold = 3f;
    public float enemyPullSpeed = 8f;
    public int collisionDamage = 15;
    public float collisionStunDuration = 1f;
    public float impactRadius = 1.2f;

    [Header("Натяжение верёвки")]
    public LayerMask groundLayer;
    public float ropeBreakTime = 1f;
    public float ropeCooldownTime = 10f;

    private LineRenderer line;
    private DistanceJoint2D joint;
    private Vector2 grapplePoint;
    private bool isGrappling;
    private Rigidbody2D rb;

    private ParticleSystem hitParticles;
    private ParticleSystem impactParticles;

    private bool isGrapplingEnemy;
    private Rigidbody2D enemyRb;
    private BanditAI enemyAI;
    private HeavyBanditBoss enemyBoss;
    private EnemyHealthBar enemyHealthBar;
    private bool isHeavyEnemy;
    private bool impactDealt;
    private float enemyGrappleTimer;

    private float strainTimer;
    private bool ropeBroken;
    private float cooldownTimer;

    private float swingMomentum;
    private float intendedRopeLength;

    public static bool isSwinging;

    private GameObject brokenPanel;
    private Text brokenText;

    void Awake()
    {
        isSwinging = false;
        foreach (var lr in GetComponents<LineRenderer>())
        {
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, transform.position);
            lr.enabled = false;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();

        var allLR = GetComponents<LineRenderer>();
        for (int i = allLR.Length - 1; i >= 1; i--) Destroy(allLR[i]);

        line = allLR.Length > 0 ? allLR[0] : gameObject.AddComponent<LineRenderer>();
        line.startWidth    = 0.06f;
        line.endWidth      = 0.03f;
        line.positionCount = 0;
        line.useWorldSpace = true;
        line.material      = new Material(Shader.Find("Sprites/Default"));
        line.startColor    = Color.black;
        line.endColor      = Color.black;
        line.enabled       = false;

        hitParticles    = BuildHitParticles();
        impactParticles = BuildImpactParticles();

        CreateBrokenUI();
    }

    ParticleSystem BuildHitParticles()
    {
        GameObject go = new GameObject("GrappleHitParticles");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startColor      = new Color(0.1f, 0.1f, 0.1f);
        main.maxParticles    = 20;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;
        return ps;
    }

    ParticleSystem BuildImpactParticles()
    {
        GameObject go = new GameObject("GrappleImpactParticles");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 9f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor      = new Color(0.9f, 0.05f, 0.05f);
        main.maxParticles    = 25;
        main.playOnAwake     = false;
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.1f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode    = ParticleSystemRenderMode.Stretch;
        rend.velocityScale = 0.12f;
        rend.lengthScale   = 2.5f;
        return ps;
    }

    void CreateBrokenUI()
    {
        Canvas canvas = FindScreenCanvas();
        if (canvas == null) return;

        brokenPanel = new GameObject("BrokenRopePanel");
        brokenPanel.transform.SetParent(canvas.transform, false);

        var rt = brokenPanel.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.zero;
        rt.pivot            = Vector2.zero;
        rt.anchoredPosition = new Vector2(20f, 20f);
        rt.sizeDelta        = new Vector2(290f, 55f);

        var bg = brokenPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0f, 0f, 0.85f);

        var textGo = new GameObject("BrokenText");
        textGo.transform.SetParent(brokenPanel.transform, false);

        brokenText = textGo.AddComponent<Text>();
        brokenText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        brokenText.fontSize  = 15;
        brokenText.color     = new Color(1f, 0.3f, 0.1f);
        brokenText.alignment = TextAnchor.MiddleCenter;

        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;

        brokenPanel.SetActive(false);
    }

    Canvas FindScreenCanvas()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceCamera) return c;
        return null;
    }

    void Update()
    {
        UpdateCooldown();

        if (Mouse.current.leftButton.wasPressedThisFrame)  TryGrapple();
        if (Mouse.current.rightButton.wasPressedThisFrame && isGrappling) StopGrapple();
        if (!isGrappling) return;

        if (isGrapplingEnemy)
            UpdateEnemyGrapple();
        else
            UpdateNormalGrapple();
    }

    void UpdateCooldown()
    {
        if (!ropeBroken) return;
        cooldownTimer -= Time.deltaTime;
        if (brokenPanel != null)
        {
            brokenPanel.SetActive(cooldownTimer > 0f);
            if (brokenText != null)
                brokenText.text = $"⚡ Верёвка порвана! Ремонт: {Mathf.CeilToInt(cooldownTimer)}с";
        }
        if (cooldownTimer <= 0f)
        {
            ropeBroken = false;
            if (brokenPanel != null) brokenPanel.SetActive(false);
        }
    }

    void UpdateNormalGrapple()
    {
        if (joint == null) return;

        bool pullIn = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
        bool letOut = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;

        if (pullIn)
            intendedRopeLength = Mathf.Max(minRopeLength, intendedRopeLength - pullSpeed * Time.deltaTime);
        else if (letOut)
            intendedRopeLength = Mathf.Min(maxRopeLength, intendedRopeLength + pullSpeed * Time.deltaTime);

        joint.distance = intendedRopeLength;

        DrawRope(transform.position, grapplePoint, intendedRopeLength);

        bool isStuck = pullIn && IsOnGround() && rb.linearVelocity.magnitude < 0.15f;
        if (isStuck)
        {
            strainTimer += Time.deltaTime;
            const float warmupTime = 3f;
            if (strainTimer > warmupTime)
            {
                float t = Mathf.Clamp01((strainTimer - warmupTime) / ropeBreakTime);
                line.startColor = Color.Lerp(Color.black, Color.red, t);
                line.endColor   = Color.Lerp(Color.black, Color.red, t);

                if (strainTimer >= warmupTime + ropeBreakTime)
                {
                    BreakRope();
                    return;
                }
            }
        }
        else
        {
            strainTimer     = 0f;
            line.startColor = Color.black;
            line.endColor   = Color.black;
        }

        float swingInput = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  swingInput = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) swingInput =  1f;

        if (swingInput != 0f)
        {
            swingMomentum = Mathf.MoveTowards(swingMomentum, 1f, Time.deltaTime * 1.5f);

            Vector2 toAnchor  = ((Vector2)grapplePoint - (Vector2)transform.position).normalized;
            Vector2 tangent   = new Vector2(toAnchor.y, -toAnchor.x);
            float   velInDir  = Vector2.Dot(rb.linearVelocity, tangent);
            float   curSpeed  = Mathf.Abs(velInDir);
            float   ratio     = curSpeed / maxSwingSpeed;
            float   speedFact = Mathf.Clamp01(1f - ratio * ratio);
            bool    canPump   = swingInput * velInDir >= -0.3f;

            if (canPump)
                rb.AddForce(tangent * swingInput * swingForce * speedFact * swingMomentum);
        }
        else
        {
            swingMomentum = Mathf.MoveTowards(swingMomentum, 0f, Time.deltaTime * 3f);
        }
    }

    void BreakRope()
    {
        StopGrapple();
        ropeBroken    = true;
        cooldownTimer = ropeCooldownTime;
        strainTimer   = 0f;
        if (brokenPanel != null) brokenPanel.SetActive(true);
    }

    bool IsOnGround()
    {
        if (groundLayer == 0) return false;
        RaycastHit2D hit = Physics2D.Raycast(
            (Vector2)transform.position, Vector2.down, 1.3f, groundLayer);
        return hit.collider != null;
    }

    void DrawRope(Vector2 start, Vector2 end, float ropeLen)
    {
        float actualDist = Vector2.Distance(start, end);
        float slack      = Mathf.Max(0f, ropeLen - actualDist);

        if (slack < 0.15f)
        {
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }
        else
        {
            const int segs = 14;
            line.positionCount = segs + 1;
            float sagAmount = Mathf.Clamp(slack * 0.45f, 0f, 3.5f);
            for (int i = 0; i <= segs; i++)
            {
                float t      = (float)i / segs;
                Vector2 flat = Vector2.Lerp(start, end, t);
                float   sag  = 4f * t * (1f - t) * sagAmount;
                line.SetPosition(i, new Vector3(flat.x, flat.y - sag, 0f));
            }
        }

        if (!line.enabled) line.enabled = true;
    }

    void UpdateEnemyGrapple()
    {
        if (enemyAI   != null && !enemyAI.gameObject)   enemyAI   = null;
        if (enemyBoss != null && !enemyBoss.gameObject) enemyBoss = null;
        if (enemyRb   != null && !enemyRb.gameObject)   enemyRb   = null;
        if (enemyRb == null && enemyAI == null && enemyBoss == null) { StopGrapple(); return; }

        grapplePoint = enemyAI   != null ? (Vector2)enemyAI.transform.position :
                       enemyBoss != null ? (Vector2)enemyBoss.transform.position :
                       enemyRb   != null ? enemyRb.position : grapplePoint;

        float ropeDist = joint != null
            ? joint.distance
            : Vector2.Distance(transform.position, grapplePoint);
        DrawRope(transform.position, grapplePoint, ropeDist);

        enemyGrappleTimer += Time.deltaTime;

        if (!isHeavyEnemy)
        {
            Vector2 dir = ((Vector2)transform.position - grapplePoint).normalized;
            if (enemyAI != null)
                enemyAI.Pull(dir * enemyPullSpeed);
            else if (enemyBoss != null && enemyRb != null)
                enemyRb.linearVelocity = dir * enemyPullSpeed * 0.7f;
            else if (enemyRb != null)
                enemyRb.linearVelocity = dir * enemyPullSpeed;
        }
        else if (joint != null)
        {
            if (enemyRb != null) joint.connectedAnchor = enemyRb.position;

            bool pullIn = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
            if (pullIn)
                joint.distance = Mathf.Max(minRopeLength, joint.distance - pullSpeed * Time.deltaTime);

            float swingInput = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  swingInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) swingInput =  1f;

            if (swingInput != 0f)
            {
                swingMomentum = Mathf.MoveTowards(swingMomentum, 1f, Time.deltaTime * 1.5f);

                Vector2 toAnchor  = (grapplePoint - (Vector2)transform.position).normalized;
                Vector2 tangent   = new Vector2(toAnchor.y, -toAnchor.x);
                float   velInDir  = Vector2.Dot(rb.linearVelocity, tangent);
                float   curSpeed  = Mathf.Abs(velInDir);
                float   ratio     = curSpeed / maxSwingSpeed;
                float   speedFact = Mathf.Clamp01(1f - ratio * ratio);
                bool    canPump   = swingInput * velInDir >= -0.3f;

                if (canPump)
                    rb.AddForce(tangent * swingInput * swingForce * speedFact * swingMomentum);
            }
            else
            {
                swingMomentum = Mathf.MoveTowards(swingMomentum, 0f, Time.deltaTime * 3f);
            }
        }

        if (!impactDealt
            && enemyGrappleTimer > 0.25f
            && Vector2.Distance(transform.position, grapplePoint) < impactRadius)
        {
            impactDealt = true;
            DealImpact();
        }
    }

    void DealImpact()
    {
        if (enemyBoss != null)
            enemyBoss.TakeDamage(collisionDamage);
        else if (enemyHealthBar != null)
            enemyHealthBar.TakeDamage(collisionDamage);
        else if (enemyAI != null)
            enemyAI.TakeDamage(collisionDamage);

        if (enemyAI != null) enemyAI.Stun(collisionStunDuration);

        impactParticles.transform.position = grapplePoint;
        impactParticles.Play();
        Invoke(nameof(StopImpactParticles), 0.6f);

        StopGrapple();
    }

    void TryGrapple()
    {
        if (ropeBroken)  return;
        if (isGrappling) { StopGrapple(); return; }

        foreach (var j in GetComponents<DistanceJoint2D>()) Destroy(j);
        joint = null;

        if (line != null)
        {
            line.positionCount = 0;
            line.enabled       = false;
            line.startColor    = Color.black;
            line.endColor      = Color.black;
        }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir        = (mouseWorld - (Vector2)transform.position).normalized;
        RaycastHit2D hit   = Physics2D.Raycast(transform.position, dir, maxDistance, grappleLayer);

        HeavyBanditBoss bossFallback = FindFirstObjectByType<HeavyBanditBoss>();
        if (bossFallback != null && !bossFallback.IsDead)
        {
            Vector2 bossPos    = bossFallback.transform.position;
            float bossDist     = Vector2.Distance(mouseWorld, bossPos);
            float playerToBoss = Vector2.Distance(transform.position, bossPos);
            if (bossDist < 3f && playerToBoss <= maxDistance + 5f)
            {
                int reaction = bossFallback.HookReaction();
                if (reaction == 2) { bossFallback.ExecuteGrabHook(); return; }
                if (reaction == 1) { bossFallback.ExecuteDodge();   return; }
                if (reaction == 0)
                {
                    enemyBoss = bossFallback;
                    enemyRb   = bossFallback.GetComponent<Rigidbody2D>();
                    enemyAI   = null;
                    enemyHealthBar = null;
                    impactDealt       = false;
                    enemyGrappleTimer = 0f;
                    isGrapplingEnemy  = true;
                    isGrappling       = true;
                    grapplePoint      = bossPos;
                    isHeavyEnemy      = false;
                    isSwinging        = false;
                    return;
                }
            }
        }

        if (hit.collider == null) return;

        Transform hitRoot = hit.collider.transform.root;
        BanditAI        bandit = hit.collider.GetComponentInParent<BanditAI>()
                              ?? hitRoot.GetComponentInChildren<BanditAI>();
        HeavyBanditBoss boss   = hit.collider.GetComponentInParent<HeavyBanditBoss>()
                              ?? hitRoot.GetComponentInChildren<HeavyBanditBoss>();
        EnemyHealthBar  hpBar  = hit.collider.GetComponentInParent<EnemyHealthBar>()
                              ?? hitRoot.GetComponentInChildren<EnemyHealthBar>();
        if (hpBar == null && bandit != null)
            hpBar = bandit.GetComponent<EnemyHealthBar>();

        if (bandit != null || boss != null || hpBar != null)
        {
            if (boss != null)
            {
                int reaction = boss.HookReaction();
                if (reaction == 2) { boss.ExecuteGrabHook(); return; }
                if (reaction == 1) { boss.ExecuteDodge();    return; }
            }

            enemyRb           = hit.collider.GetComponentInParent<Rigidbody2D>()
                             ?? hitRoot.GetComponentInChildren<Rigidbody2D>();
            enemyAI           = bandit;
            enemyBoss         = boss;
            enemyHealthBar    = hpBar;
            impactDealt       = false;
            enemyGrappleTimer = 0f;
            isGrapplingEnemy  = true;
            isGrappling       = true;
            grapplePoint      = hit.point;

            isHeavyEnemy = bandit == null && boss == null && (enemyRb == null || enemyRb.mass >= enemyMassThreshold);
            isSwinging   = isHeavyEnemy;

            if (isHeavyEnemy)
            {
                joint = gameObject.AddComponent<DistanceJoint2D>();
                joint.autoConfigureDistance = false;
                joint.enableCollision       = true;
                joint.maxDistanceOnly       = true;
                joint.connectedAnchor       = hit.point;
                joint.distance = Vector2.Distance(transform.position, hit.point);
            }
        }
        else
        {
            grapplePoint     = hit.point;
            isGrappling      = true;
            isGrapplingEnemy = false;
            strainTimer      = 0f;

            joint = gameObject.AddComponent<DistanceJoint2D>();
            joint.autoConfigureDistance = false;
            joint.enableCollision       = true;
            joint.maxDistanceOnly       = true;
            joint.connectedAnchor       = grapplePoint;
            joint.distance     = Vector2.Distance(transform.position, grapplePoint);
            intendedRopeLength = joint.distance;

            isSwinging = true;
            SpawnHitParticles(hit);
        }
    }

    void SpawnHitParticles(RaycastHit2D hit)
    {
        var main = hitParticles.main;
        main.startColor = new Color(0.1f, 0.1f, 0.1f);
        hitParticles.transform.position = grapplePoint;
        hitParticles.Play();
        Invoke(nameof(StopHitParticles), 0.7f);
    }

    void StopHitParticles()    { hitParticles.Stop();    hitParticles.Clear(); }
    void StopImpactParticles() { impactParticles.Stop(); impactParticles.Clear(); }

    void StopGrapple()
    {
        isGrappling      = false;
        isGrapplingEnemy = false;
        isSwinging       = false;
        enemyRb          = null;
        enemyAI          = null;
        enemyBoss        = null;
        enemyHealthBar   = null;
        strainTimer      = 0f;
        swingMomentum    = 0f;
        line.positionCount = 0;
        line.enabled     = false;
        line.startColor  = Color.black;
        line.endColor    = Color.black;
        if (joint != null) { Destroy(joint); joint = null; }
        intendedRopeLength = 0f;
        if (rb != null && rb.linearVelocity.magnitude > 10f)
            rb.linearVelocity = rb.linearVelocity.normalized * 10f;
    }
}
