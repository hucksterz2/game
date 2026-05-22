using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    public static int permanentCoins = 0;
    public static int currentLevelCoins = 0;
    public static int Coins => permanentCoins + currentLevelCoins;

    [Header("Текст счётчика")]
    public string coinLabel = "💰 Монет: ";

    private Text coinText;
    private GameObject canvasObj;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentLevelCoins = 0;
        UpdateText();
    }

    void BuildUI()
    {
        canvasObj = new GameObject("CoinCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject txt = new GameObject("CoinText");
        txt.transform.SetParent(canvasObj.transform, false);
        coinText = txt.AddComponent<Text>();
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinText.fontSize = 36;
        coinText.color = new Color(1f, 0.9f, 0.2f);
        coinText.alignment = TextAnchor.MiddleRight;
        coinText.fontStyle = FontStyle.Bold;
        coinText.text = coinLabel + Coins;
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
        currentLevelCoins += amount;
        UpdateText();
    }

    public static bool TrySpend(int amount)
    {
        if (Coins < amount) return false;

        int fromCurrent = Mathf.Min(currentLevelCoins, amount);
        currentLevelCoins -= fromCurrent;
        permanentCoins -= (amount - fromCurrent);
        UpdateText();
        return true;
    }

    public static void CommitLevelCoins()
    {
        permanentCoins += currentLevelCoins;
        currentLevelCoins = 0;
        UpdateText();
    }

    public static void ResetAll()
    {
        permanentCoins = 0;
        currentLevelCoins = 0;
        UpdateText();
    }

    static void UpdateText()
    {
        if (Instance != null && Instance.coinText != null)
            Instance.coinText.text = Instance.coinLabel + Coins;
    }
}