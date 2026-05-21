using UnityEngine;
using UnityEngine.UI;

public class BossHPBar : MonoBehaviour
{
    public string bossName = "Главарь бандитов";

    Image      fillImage;
    GameObject panel;

    void Awake()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
        BuildUI();
        panel.SetActive(false);
    }

    void BuildUI()
    {
        panel = new GameObject("BossPanel");
        panel.transform.SetParent(transform, false);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        Anch(panel.GetComponent<RectTransform>(), 0.08f, 0.03f, 0.92f, 0.13f);

        MakeText(panel.transform, bossName, new Vector2(0f, 0.56f), Vector2.one,
            20, new Color(1f, 0.82f, 0.2f), FontStyle.Bold);

        var barBg = new GameObject("BarBG");
        barBg.transform.SetParent(panel.transform, false);
        barBg.AddComponent<Image>().color = new Color(0.1f, 0f, 0f);
        Anch(barBg.GetComponent<RectTransform>(), 0.01f, 0.06f, 0.99f, 0.55f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(barBg.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.1f, 0.1f);
        var rt = fill.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    static void Anch(RectTransform rt, float x0, float y0, float x1, float y1)
    { rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.sizeDelta = Vector2.zero; }

    static void MakeText(Transform parent, string text, Vector2 aMin, Vector2 aMax,
        int size, Color color, FontStyle style)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false);
        var t  = go.AddComponent<Text>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.sizeDelta = Vector2.zero;
    }

    public void SetVisible(bool v) { if (panel != null) panel.SetActive(v); }

    public void UpdateHP(float ratio)
    {
        if (fillImage == null) return;
        fillImage.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fillImage.color = Color.Lerp(new Color(0.85f, 0.1f, 0.1f), new Color(1f, 0.55f, 0f), 1f - Mathf.Clamp01(ratio));
    }
}
