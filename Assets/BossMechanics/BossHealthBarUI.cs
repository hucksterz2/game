using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    public BossHealth bossHealth;

    [Header("Settings")]
    public string bossName = "Болотный Босс";
    public bool showOnStart = false;

    private Image fillImage;
    private Image delayedImage;
    private TextMeshProUGUI hpText;
    private TextMeshProUGUI nameText;
    private GameObject panel;

    private float delayedRatio = 1f;
    private float delayTimer = 0f;

    void Start()
    {
        CreateBossBar();
        if (!showOnStart && panel != null)
            panel.SetActive(false);
    }

    public void ShowBar()
    {
        if (panel != null)
            panel.SetActive(true);
    }

    public void HideBar()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void CreateBossBar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("BossHealthBarUI: Canvas не найден!");
            return;
        }

        panel = new GameObject("BossHPPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 60);
        panelRect.sizeDelta = new Vector2(900, 110);

        Image outerFrame = panel.AddComponent<Image>();
        outerFrame.color = new Color(0.65f, 0.5f, 0.2f, 1f);

        GameObject innerBg = new GameObject("InnerBg");
        innerBg.transform.SetParent(panel.transform, false);
        Image innerBgImg = innerBg.AddComponent<Image>();
        innerBgImg.color = new Color(0.05f, 0.03f, 0.03f, 0.95f);
        RectTransform innerRect = innerBg.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-8, -8);
        innerRect.anchoredPosition = Vector2.zero;

        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(innerBg.transform, false);
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = bossName;
        nameText.fontSize = 32;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(0.95f, 0.85f, 0.55f);
        nameText.outlineColor = new Color(0, 0, 0, 1);
        nameText.outlineWidth = 0.2f;
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.pivot = new Vector2(0.5f, 1);
        nameRect.anchoredPosition = new Vector2(0, -5);
        nameRect.sizeDelta = new Vector2(0, 45);

        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(innerBg.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.12f, 0.05f, 0.05f);
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0, 0);
        barBgRect.anchorMax = new Vector2(1, 0);
        barBgRect.pivot = new Vector2(0.5f, 0);
        barBgRect.anchoredPosition = new Vector2(0, 15);
        barBgRect.sizeDelta = new Vector2(-50, 35);

        GameObject delayed = new GameObject("DelayedFill");
        delayed.transform.SetParent(barBg.transform, false);
        delayedImage = delayed.AddComponent<Image>();
        delayedImage.color = new Color(1f, 0.95f, 0.7f, 0.85f);
        RectTransform delayedRect = delayed.GetComponent<RectTransform>();
        delayedRect.anchorMin = Vector2.zero;
        delayedRect.anchorMax = Vector2.one;
        delayedRect.sizeDelta = new Vector2(-4, -4);
        delayedRect.anchoredPosition = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barBg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.1f, 0.1f, 1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4, -4);
        fillRect.anchoredPosition = Vector2.zero;

        GameObject hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(barBg.transform, false);
        hpText = hpTextObj.AddComponent<TextMeshProUGUI>();
        hpText.fontSize = 20;
        hpText.fontStyle = FontStyles.Bold;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
        hpText.outlineColor = new Color(0, 0, 0, 1);
        hpText.outlineWidth = 0.25f;
        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;
        hpTextRect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (bossHealth == null || fillImage == null) return;

        float ratio = (float)bossHealth.currentHP / bossHealth.maxHP;

        fillImage.rectTransform.anchorMax = new Vector2(
            Mathf.Lerp(fillImage.rectTransform.anchorMax.x, ratio, Time.deltaTime * 10f), 1);

        if (ratio < delayedRatio)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer > 0.5f)
                delayedRatio = Mathf.Lerp(delayedRatio, ratio, Time.deltaTime * 2f);
        }
        else
        {
            delayedRatio = ratio;
            delayTimer = 0f;
        }

        delayedImage.rectTransform.anchorMax = new Vector2(delayedRatio, 1);

        if (hpText != null)
            hpText.text = bossHealth.currentHP + " / " + bossHealth.maxHP;

        if (bossHealth.IsDead() && panel != null)
            Destroy(panel, 2f);
    }
}