using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelExit : MonoBehaviour
{
    [Header("Имя следующей сцены (должна быть в Build Profiles)")]
    public string nextSceneName = "Level2";

    [Header("Радиус срабатывания")]
    public float radius = 1.5f;

    [Header("Длительность затемнения (сек)")]
    public float fadeDuration = 0.6f;

    [Header("Показывать прогресс загрузки?")]
    public bool showLoadingText = true;

    [Header("Подсказка над триггером (необязательно)")]
    public string promptText = "";
    public int fontSize = 28;

    private bool isLoading = false;
    private Transform player;
    private GameObject promptObj;
    private GameObject fadeObj;
    private Image fadeImage;
    private Text loadingText;

    void Start()
    {
        if (!string.IsNullOrEmpty(promptText)) BuildPrompt();
        BuildFade();
    }

    void BuildPrompt()
    {
        GameObject canvasObj = new GameObject("ExitCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        promptObj = new GameObject("ExitPrompt");
        promptObj.transform.SetParent(canvas.transform, false);
        Text t = promptObj.AddComponent<Text>();
        t.text = promptText;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        RectTransform r = promptObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, 0.75f);
        r.anchorMax = new Vector2(0.95f, 0.75f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(0, 60);
        r.anchoredPosition = Vector2.zero;
        promptObj.SetActive(false);
    }

    void BuildFade()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2500;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        fadeObj = new GameObject("Fade");
        fadeObj.transform.SetParent(canvas.transform, false);
        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        RectTransform fr = fadeObj.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = fr.offsetMax = Vector2.zero;

        if (showLoadingText)
        {
            GameObject lt = new GameObject("LoadingText");
            lt.transform.SetParent(fadeObj.transform, false);
            loadingText = lt.AddComponent<Text>();
            loadingText.text = "Загрузка...";
            loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            loadingText.fontSize = 48;
            loadingText.color = new Color(1, 1, 1, 0);
            loadingText.alignment = TextAnchor.MiddleCenter;
            loadingText.fontStyle = FontStyle.Bold;
            RectTransform lr = lt.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
        }
    }

    bool PlayerNear()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return false;
            player = p.transform.root;
        }
        return Vector2.Distance(transform.position, player.position) <= radius;
    }

    void Update()
    {
        if (isLoading) return;
        if (PlayerNear())
        {
            if (promptObj != null) promptObj.SetActive(true);
            StartCoroutine(LoadNext());
        }
        else
        {
            if (promptObj != null) promptObj.SetActive(false);
        }
    }

    IEnumerator LoadNext()
    {
        isLoading = true;
        if (promptObj != null) promptObj.SetActive(false);

        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            if (loadingText != null) loadingText.color = new Color(1, 1, 1, a);
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}