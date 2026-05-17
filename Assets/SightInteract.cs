using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CircleCollider2D))]
public class SightInteract : MonoBehaviour
{
    public enum DisplayMode { SimpleText, DialogueWindow }

    [Header("–≈∆»Ã Œ“Œ¡–¿∆≈Õ»ﬂ")]
    public DisplayMode mode = DisplayMode.SimpleText;

    [Header("“≈ —“")]
    [TextArea(3, 10)] public string signText = "Hello!";
    public string promptText = "[ E ] Read";

    [Header("ƒÀﬂ DialogueWindow")]
    public Sprite portrait;
    public bool splitByDoubleNewline = true;

    [Header("ƒÀﬂ SimpleText")]
    public int fontSize = 32;
    public Color textColor = Color.yellow;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;
    [Range(0f, 1f)] public float messagePosY = 0.85f;

    private bool playerInZone = false;
    private bool isReading = false;
    private GameObject promptObj;
    private GameObject messageObj;

    void Start()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        promptObj = MakeText(canvas.transform, promptText, promptColor, promptPosY);
        if (mode == DisplayMode.SimpleText)
            messageObj = MakeText(canvas.transform, signText, textColor, messagePosY);

        promptObj.SetActive(false);
        if (messageObj != null) messageObj.SetActive(false);
    }

    GameObject MakeText(Transform parent, string txt, Color color, float anchorY)
    {
        GameObject go = new GameObject("SignText");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, anchorY);
        r.anchorMax = new Vector2(0.95f, anchorY);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(0, 200);
        r.anchoredPosition = Vector2.zero;
        return go;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            playerInZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            playerInZone = false;
            isReading = false;
            if (messageObj != null) messageObj.SetActive(false);
        }
    }

    void Update()
    {
        bool dialogActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;

        if (mode == DisplayMode.DialogueWindow)
        {
            if (playerInZone && !dialogActive)
            {
                promptObj.SetActive(true);
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    promptObj.SetActive(false);
                    StartDialog();
                }
            }
            else promptObj.SetActive(false);
        }
        else
        {
            if (playerInZone && !isReading)
            {
                promptObj.SetActive(true);
                if (messageObj != null) messageObj.SetActive(false);
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    isReading = true;
                    promptObj.SetActive(false);
                    if (messageObj != null) messageObj.SetActive(true);
                }
            }
            else if (playerInZone && isReading)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    isReading = false;
                    if (messageObj != null) messageObj.SetActive(false);
                    promptObj.SetActive(true);
                }
            }
            else
            {
                promptObj.SetActive(false);
                if (messageObj != null) messageObj.SetActive(false);
            }
        }
    }

    void StartDialog()
    {
        string[] lines;
        if (splitByDoubleNewline)
            lines = signText.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        else
            lines = new string[] { signText };

        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
        DialogueManager.Instance.StartDialogue(portrait, lines);
    }
}