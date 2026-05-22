using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("��� ����� �������� ����")]
    public string mainMenuScene = "MainMenu";

    private GameObject panel;
    private bool isPaused = false;
    private bool settingsWasOpen = false;
    private SettingsMenu settingsMenu;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();

        settingsMenu = FindFirstObjectByType<SettingsMenu>();
        if (settingsMenu == null)
        {
            GameObject sm = new GameObject("SettingsMenu");
            settingsMenu = sm.AddComponent<SettingsMenu>();
        }
    }

    void EnsureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject go = new GameObject("EventSystem");
            es = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            return;
        }

        var oldModules = es.GetComponents<BaseInputModule>();
        foreach (var m in oldModules)
            if (!(m is InputSystemUIInputModule)) Destroy(m);

        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (settingsWasOpen && settingsMenu != null && !settingsMenu.IsOpen)
        {
            settingsWasOpen = false;
            if (isPaused) panel.SetActive(true);
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        panel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void Continue()
    {
        isPaused = false;
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void OpenSettings()
    {
        if (settingsMenu == null) return;
        panel.SetActive(false);
        settingsMenu.OpenSettings();
        settingsWasOpen = true;
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        AddTitle("ПАУЗА", 0.78f);
        AddButton("Продолжить", 0.55f, Continue, new Color(0.2f, 0.5f, 0.3f));
        AddButton("Настройки", 0.38f, OpenSettings, new Color(0.25f, 0.4f, 0.6f));
        AddButton("Главное меню", 0.21f, GoToMainMenu, new Color(0.5f, 0.2f, 0.2f));    

        panel.SetActive(false);
    }

    void AddTitle(string txt, float y)
    {
        GameObject go = new GameObject("Title");
        go.transform.SetParent(panel.transform, false);
        Text t = go.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 90;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.2f, y);
        r.anchorMax = new Vector2(0.8f, y + 0.1f);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void AddButton(string txt, float y, System.Action onClick, Color color)
    {
        GameObject go = new GameObject("Button_" + txt);
        go.transform.SetParent(panel.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f);
        colors.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
        btn.colors = colors;

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.35f, y);
        r.anchorMax = new Vector2(0.65f, y + 0.10f);
        r.offsetMin = r.offsetMax = Vector2.zero;

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        Text t = txtGo.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 36;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        RectTransform tr = txtGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
    }
}