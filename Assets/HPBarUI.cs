using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Ссылки")]
    public PlayerHealth playerHealth;

    private Image fillImage;
    private Text hpText;

    void Start()
    {
        // Авто-поиск PlayerHealth если не назначен в инспекторе
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        CreateHPBar();
    }

    Canvas FindScreenCanvas()
    {
        // Ищем Screen Space холст, а не World Space (у врагов он World Space)
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceCamera) return c;
        return null;
    }

    void CreateHPBar()
    {
        Canvas canvas = FindScreenCanvas();
        if (canvas == null) return;

        GameObject panel = new GameObject("HPPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(20, -20);
        panelRect.sizeDelta = new Vector2(220, 50);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.6f);

        GameObject heartObj = new GameObject("HeartIcon");
        heartObj.transform.SetParent(panel.transform, false);
        Text heartText = heartObj.AddComponent<Text>();
        heartText.text = "♥";
        heartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        heartText.fontSize = 28;
        heartText.color = new Color(0.9f, 0.15f, 0.15f);
        heartText.alignment = TextAnchor.MiddleCenter;
        RectTransform heartRect = heartObj.GetComponent<RectTransform>();
        heartRect.anchorMin = new Vector2(0, 0);
        heartRect.anchorMax = new Vector2(0, 1);
        heartRect.anchoredPosition = new Vector2(24, 0);
        heartRect.sizeDelta = new Vector2(35, 0);

        GameObject barBg = new GameObject("BarBackground");
        barBg.transform.SetParent(panel.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.05f, 0.05f);
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0, 0.5f);
        barBgRect.anchorMax = new Vector2(1, 0.5f);
        barBgRect.anchoredPosition = new Vector2(20, 0);
        barBgRect.sizeDelta = new Vector2(-55, 14);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barBg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.1f, 0.1f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("HPText");
        textObj.transform.SetParent(panel.transform, false);
        hpText = textObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 13;
        hpText.color = Color.white;
        hpText.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.anchoredPosition = new Vector2(20, 0);
        textRect.sizeDelta = new Vector2(-45, 0);
    }

    void Update()
    {
        if (playerHealth == null || fillImage == null) return;

        float ratio = (float)playerHealth.currentHP / playerHealth.maxHP;

        fillImage.rectTransform.anchorMax = new Vector2(
            Mathf.Lerp(fillImage.rectTransform.anchorMax.x, ratio, Time.deltaTime * 8f),
            1);

        fillImage.color = Color.Lerp(
            new Color(0.85f, 0.1f, 0.1f),
            new Color(0.1f, 0.75f, 0.2f),
            ratio);

        if (hpText != null)
            hpText.text = playerHealth.currentHP + " / " + playerHealth.maxHP;
    }
}
