using UnityEngine;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;
    public static int Coins = 0;

    private Text coinText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("CoinCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject txt = new GameObject("CoinText");
        txt.transform.SetParent(canvas.transform, false);
        coinText = txt.AddComponent<Text>();
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinText.fontSize = 36;
        coinText.color = new Color(1f, 0.9f, 0.2f);
        coinText.alignment = TextAnchor.MiddleRight;
        coinText.fontStyle = FontStyle.Bold;
        coinText.text = "Coins: " + Coins;
        coinText.raycastTarget = false;

        var shadow = txt.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.7f);
        shadow.effectDistance = new Vector2(2, -2);

        RectTransform r = txt.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(1, 1);
        r.sizeDelta = new Vector2(300, 60);
        r.anchoredPosition = new Vector2(-20, -20);
    }

    public static void Add(int amount)
    {
        Coins += amount;
        if (Instance != null && Instance.coinText != null)
            Instance.coinText.text = "Coins: " + Coins;
    }

    public static bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        if (Instance != null && Instance.coinText != null)
            Instance.coinText.text = "Coins: " + Coins;
        return true;
    }
}