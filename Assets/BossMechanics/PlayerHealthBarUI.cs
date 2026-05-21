using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    private Image fillImage;
    private TextMeshProUGUI hpText;

    void Start()
    {
        CreateBar();
    }

    void CreateBar()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("PlayerHPPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(400, 110);

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

        GameObject nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(innerBg.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "BURLY MAN";
        nameText.fontSize = 28;
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
        nameRect.sizeDelta = new Vector2(0, 40);

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

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barBg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.1f, 0.75f, 0.2f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4, -4);
        fillRect.anchoredPosition = Vector2.zero;

        GameObject hpTextObj = new GameObject("HPText");
        hpTextObj.transform.SetParent(barBg.transform, false);
        hpText = hpTextObj.AddComponent<TextMeshProUGUI>();
        hpText.fontSize = 18;
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
        if (playerHealth == null || fillImage == null) return;

        float ratio = (float)playerHealth.currentHP / playerHealth.maxHP;

        fillImage.rectTransform.anchorMax = new Vector2(
            Mathf.Lerp(fillImage.rectTransform.anchorMax.x, ratio, Time.deltaTime * 8f), 1);

        fillImage.color = Color.Lerp(
            new Color(0.85f, 0.1f, 0.1f),
            new Color(0.1f, 0.75f, 0.2f),
            ratio);

        if (hpText != null)
            hpText.text = playerHealth.currentHP + " / " + playerHealth.maxHP;
    }
}