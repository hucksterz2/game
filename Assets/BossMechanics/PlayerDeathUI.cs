using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerDeathUI : MonoBehaviour
{
    public PlayerHealth playerHealth;

    private GameObject panel;
    private bool shown = false;

    void Start()
    {
        CreateDeathScreen();
    }

    void Update()
    {
        if (!shown && playerHealth != null && playerHealth.currentHP <= 0)
        {
            shown = true;
            ShowDeathScreen();
        }
    }

    void CreateDeathScreen()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        panel = new GameObject("DeathPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
        title.text = "ВЫ УМЕРЛИ";
        title.fontSize = 80;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.85f, 0.1f, 0.1f);
        title.outlineColor = new Color(0, 0, 0, 1);
        title.outlineWidth = 0.3f;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 80);
        titleRect.sizeDelta = new Vector2(0, 120);

        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(panel.transform, false);
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.65f, 0.5f, 0.2f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(RestartGame);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -40);
        btnRect.sizeDelta = new Vector2(300, 65);

        GameObject btnTextObj = new GameObject("ButtonText");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "НАЧАТЬ ЗАНОВО";
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = new Color(0.05f, 0.03f, 0.03f);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        btnTextRect.anchoredPosition = Vector2.zero;

        panel.SetActive(false);
    }

    void ShowDeathScreen()
    {
        if (panel != null)
            panel.SetActive(true);
        Time.timeScale = 0f;
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SecondScene");
    }
}