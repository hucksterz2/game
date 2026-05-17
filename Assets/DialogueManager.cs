using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("—корость печати (сек на букву)")]
    public float typeSpeed = 0.03f;

    private GameObject panel;
    private Image portraitImage;
    private Text bodyText;
    private Text continueHint;

    private DialogueLine[] currentLines;
    private int currentIndex;
    private bool isTyping;
    private string fullLine;
    private bool dialogueActive;

    public bool IsActive => dialogueActive;

    [System.Serializable]
    public struct DialogueLine
    {
        public Sprite portrait;
        [TextArea(2, 5)] public string text;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("DialoguePanel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.05f, 0.05f);
        pr.anchorMax = new Vector2(0.95f, 0.28f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        GameObject port = new GameObject("Portrait");
        port.transform.SetParent(panel.transform, false);
        portraitImage = port.AddComponent<Image>();
        portraitImage.color = Color.white;
        RectTransform pir = port.GetComponent<RectTransform>();
        pir.anchorMin = new Vector2(0, 0);
        pir.anchorMax = new Vector2(0, 1);
        pir.pivot = new Vector2(0, 0.5f);
        pir.sizeDelta = new Vector2(180, 0);
        pir.anchoredPosition = new Vector2(20, 0);

        GameObject txt = new GameObject("BodyText");
        txt.transform.SetParent(panel.transform, false);
        bodyText = txt.AddComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyText.fontSize = 24;
        bodyText.color = Color.white;
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = new Vector2(220, 50);
        tr.offsetMax = new Vector2(-30, -20);

        GameObject hint = new GameObject("Hint");
        hint.transform.SetParent(panel.transform, false);
        continueHint = hint.AddComponent<Text>();
        continueHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        continueHint.fontSize = 18;
        continueHint.color = new Color(1, 1, 1, 0.6f);
        continueHint.text = "[ E Ч далее ]";
        continueHint.alignment = TextAnchor.LowerRight;
        RectTransform hr = hint.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 0);
        hr.anchorMax = new Vector2(1, 0);
        hr.pivot = new Vector2(1, 0);
        hr.offsetMin = new Vector2(0, 5);
        hr.offsetMax = new Vector2(-15, 30);

        panel.SetActive(false);
    }

    public void StartDialogue(Sprite portrait, string[] lines)
    {
        DialogueLine[] dl = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            dl[i] = new DialogueLine { portrait = portrait, text = lines[i] };
        }
        StartDialogue(dl);
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        currentLines = lines;
        currentIndex = 0;
        dialogueActive = true;
        panel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        DialogueLine line = currentLines[currentIndex];
        if (line.portrait != null) { portraitImage.sprite = line.portrait; portraitImage.enabled = true; }
        else portraitImage.enabled = false;

        isTyping = true;
        fullLine = line.text;
        bodyText.text = "";
        foreach (char c in fullLine)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }

    void Update()
    {
        if (!dialogueActive) return;
        bool advance = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (advance)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                bodyText.text = fullLine;
                isTyping = false;
            }
            else
            {
                currentIndex++;
                if (currentIndex >= currentLines.Length)
                {
                    panel.SetActive(false);
                    dialogueActive = false;
                }
                else StartCoroutine(TypeLine());
            }
        }
    }
}