using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PlayerHealth : MonoBehaviour
{
    public int   maxHP       = 100;
    public int   currentHP;
    public float regenDelay  = 10f;
    public int   regenAmount = 5;

    private PlayerController pc;
    private float noHitTimer;
    private float regenTimer;

    void Start()
    {
        currentHP = maxHP;
        pc = GetComponent<PlayerController>();
        if (pc == null) pc = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        if (isDead || currentHP <= 0) return;
        noHitTimer += Time.deltaTime;
        if (noHitTimer >= regenDelay && currentHP < maxHP)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1f)
            {
                regenTimer -= 1f;
                currentHP   = Mathf.Min(maxHP, currentHP + regenAmount);
            }
        }
        else
        {
            regenTimer = 0f;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead || currentHP <= 0) return;
        noHitTimer = 0f;

        if (pc == null)
        {
            pc = GetComponent<PlayerController>();
            if (pc == null) pc = GetComponentInParent<PlayerController>();
        }

        if (pc != null)
        {
            if (pc.IsDashInvincible) return;

            if (pc.ParryActive)
            {
                pc.DoParry();
                return;
            }

            if (pc.IsBlocking)
                amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - pc.blockDamageReduction)));
        }

        currentHP = Mathf.Max(0, currentHP - amount);

        BloodParticles.Spawn(transform.position + Vector3.up * 0.5f, amount, transform);

        RageSystem rage = GetComponent<RageSystem>() ?? GetComponentInParent<RageSystem>();
        if (rage != null) rage.AddRage(amount);

        if (currentHP > 0)
        {
            Animator hitAnim = GetComponentInChildren<Animator>();
            if (hitAnim != null)
                foreach (var p in hitAnim.parameters)
                    if (p.name == "Hit" && p.type == AnimatorControllerParameterType.Trigger)
                    { hitAnim.SetTrigger("Hit"); break; }
        }

        if (currentHP <= 0) Die();
    }

    bool isDead;

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (pc != null) pc.IsDead = true;

        var rb2 = GetComponent<Rigidbody2D>() ?? GetComponentInParent<Rigidbody2D>();
        if (rb2 != null) rb2.linearVelocity = new Vector2(0f, rb2.linearVelocity.y);

        Animator deathAnim = GetComponentInChildren<Animator>();
        if (deathAnim != null)
        {
            foreach (var p in deathAnim.parameters)
                if (p.name == "Die" && p.type == AnimatorControllerParameterType.Trigger)
                {
                    deathAnim.ResetTrigger("Die");
                    deathAnim.SetTrigger("Die");
                    break;
                }

            StartCoroutine(FreezeAnimAfter(deathAnim, 0.85f));
        }

        StartCoroutine(DieRoutine());
    }

    System.Collections.IEnumerator DieRoutine()
    {
        yield return new WaitForSecondsRealtime(1.1f);
        Time.timeScale = 0f;
        ShowDeathScreen();
    }

    System.Collections.IEnumerator FreezeAnimAfter(Animator a, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (a != null)
        {
            a.speed = 0f;
        }
    }

    void ShowDeathScreen()
    {
        GameObject canvasGO = new GameObject("DeathCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
        { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>(); }

        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvas.transform, false);
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        var or = overlay.GetComponent<RectTransform>();
        or.anchorMin = Vector2.zero; or.anchorMax = Vector2.one; or.sizeDelta = Vector2.zero;

        GameObject panel = new GameObject("DeathPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.AddComponent<Image>().color = new Color(0.04f, 0f, 0f, 0.97f);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.anchoredPosition = Vector2.zero; pr.sizeDelta = new Vector2(460f, 290f);

        MakeText(panel.transform, "Title", "ВЫ УМЕРЛИ",
            new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.97f),
            58, new Color(0.88f, 0.05f, 0.05f), FontStyle.Bold, addShadow: true);

        MakeText(panel.transform, "Subtitle", "Ваше приключение закончилось...",
            new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.60f),
            19, new Color(0.65f, 0.32f, 0.32f), FontStyle.Normal);

        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panel.transform, false);
        Image btnImg = btnGO.AddComponent<Image>(); btnImg.color = new Color(0.50f, 0.04f, 0.04f);
        Button btn = btnGO.AddComponent<Button>(); btn.targetGraphic = btnImg;
        var cb = btn.colors;
        cb.normalColor      = new Color(0.50f, 0.04f, 0.04f);
        cb.highlightedColor = new Color(0.70f, 0.10f, 0.10f);
        cb.pressedColor     = new Color(0.30f, 0.01f, 0.01f);
        btn.colors = cb; btn.onClick.AddListener(RestartGame);
        var br = btnGO.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.20f, 0.07f); br.anchorMax = new Vector2(0.80f, 0.33f); br.sizeDelta = Vector2.zero;
        MakeText(btnGO.transform, "BtnText", "Начать заново",
            Vector2.zero, Vector2.one, 23, Color.white, FontStyle.Normal);
    }

    void MakeText(Transform parent, string name, string content,
        Vector2 aMin, Vector2 aMax, int size, Color color, FontStyle style,
        bool addShadow = false)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size; t.color = color; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        if (addShadow) { var s = go.AddComponent<Shadow>(); s.effectColor = new Color(0,0,0,0.7f); s.effectDistance = new Vector2(3,-3); }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.sizeDelta = Vector2.zero;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        var a = GetComponentInChildren<Animator>();
        if (a != null) a.speed = 1f;
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }
}
