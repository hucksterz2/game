using UnityEngine;
using UnityEngine.UI;

public class RageSystem : MonoBehaviour
{
    [Header("Ярость")]
    public float maxRage           = 100f;
    public float ragePerDamageUnit = 4f;

    [Header("Берсерк")]
    public float berserkDuration   = 8f;
    public float berserkSpeedMult  = 2f;
    public float berserkDamageMult = 2f;

    public bool  IsBerserk        { get; private set; }
    public float DamageMultiplier => IsBerserk ? berserkDamageMult : 1f;
    public float SpeedMultiplier  => IsBerserk ? berserkSpeedMult  : 1f;

    private float currentRage;
    private float displayedRage;
    private float berserkTimer;

    private Image rageFill;
    private Image rageBg;
    private Text  rageLabel;
    private Image berserkGlow;

    private SpriteRenderer playerSR;
    private Coroutine flashRoutine;
    private static readonly Color berserkTint = new Color(1f, 0.28f, 0.28f);

    void Start()
    {
        playerSR = GetComponentInChildren<SpriteRenderer>();
        BuildUI();
    }

    void Update()
    {
        if (IsBerserk)
        {
            berserkTimer -= Time.deltaTime;
            if (berserkTimer <= 0f) ExitBerserk();
        }

        displayedRage = Mathf.MoveTowards(displayedRage, currentRage, Time.deltaTime * maxRage * 0.35f);

        if (rageFill != null)
        {
            rageFill.fillAmount = displayedRage / maxRage;

            float t = currentRage / maxRage;
            rageFill.color = IsBerserk
                ? new Color(1f, 0.15f, 0.05f)
                : Color.Lerp(new Color(0.95f, 0.55f, 0.05f), new Color(0.95f, 0.1f, 0.05f), t);
        }

        if (rageLabel != null)
        {
            if (IsBerserk)
            {
                float blink = Mathf.Sin(Time.unscaledTime * 8f) > 0f ? 1f : 0.4f;
                rageLabel.color = new Color(1f, 0.2f, 0.2f, blink);
                rageLabel.text  = $"БЕРСЕРК  {Mathf.CeilToInt(berserkTimer)}с";
            }
            else
            {
                rageLabel.color = new Color(0.85f, 0.6f, 0.1f);
                rageLabel.text  = "ЯРОСТЬ";
            }
        }

        if (berserkGlow != null) berserkGlow.gameObject.SetActive(IsBerserk);
    }

    public void AddRage(int damageDealt)
    {
        if (IsBerserk) return;
        currentRage = Mathf.Min(maxRage, currentRage + damageDealt * ragePerDamageUnit);
        if (currentRage >= maxRage) EnterBerserk();
    }

    void EnterBerserk()
    {
        IsBerserk    = true;
        berserkTimer = berserkDuration;
        currentRage  = maxRage;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(BerserkFlashRoutine());
    }

    void ExitBerserk()
    {
        IsBerserk   = false;
        currentRage = 0f;
        if (playerSR != null) playerSR.color = Color.white;
    }

    System.Collections.IEnumerator BerserkFlashRoutine()
    {
        for (int i = 0; i < 7; i++)
        {
            if (playerSR != null) playerSR.color = berserkTint;
            yield return new WaitForSeconds(0.05f);
            if (playerSR != null) playerSR.color = Color.white;
            yield return new WaitForSeconds(0.05f);
        }
        if (IsBerserk && playerSR != null)
            playerSR.color = new Color(1f, 0.75f, 0.75f);
    }

    void BuildUI()
    {
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) return;

        var labelGO = new GameObject("RageLabel");
        labelGO.transform.SetParent(canvas.transform, false);
        rageLabel           = labelGO.AddComponent<Text>();
        rageLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rageLabel.fontSize  = 12;
        rageLabel.fontStyle = FontStyle.Bold;
        rageLabel.color     = new Color(0.85f, 0.6f, 0.1f);
        rageLabel.alignment = TextAnchor.MiddleLeft;
        rageLabel.text      = "ЯРОСТЬ";
        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = lrt.pivot = Vector2.zero;
        lrt.anchoredPosition = new Vector2(14f, 144f);
        lrt.sizeDelta        = new Vector2(120f, 16f);

        var panel = new GameObject("RageBarBg");
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(14f, 128f);
        rt.sizeDelta        = new Vector2(160f, 14f);

        rageBg       = panel.AddComponent<Image>();
        rageBg.color = new Color(0.08f, 0.03f, 0.03f, 0.92f);

        var fillGO = new GameObject("RageFill");
        fillGO.transform.SetParent(panel.transform, false);
        rageFill            = fillGO.AddComponent<Image>();
        rageFill.color      = new Color(0.95f, 0.45f, 0.05f);
        rageFill.type       = Image.Type.Filled;
        rageFill.fillMethod = Image.FillMethod.Horizontal;
        rageFill.fillAmount = 0f;
        var fr = fillGO.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.sizeDelta = Vector2.zero;

        var glowGO = new GameObject("BerserkScreenGlow");
        glowGO.transform.SetParent(canvas.transform, false);
        berserkGlow       = glowGO.AddComponent<Image>();
        berserkGlow.color = new Color(0.8f, 0f, 0f, 0.18f);
        var gr = glowGO.GetComponent<RectTransform>();
        gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one; gr.sizeDelta = Vector2.zero;
        glowGO.SetActive(false);

        glowGO.transform.SetSiblingIndex(0);
    }
}
