using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SightInteract : MonoBehaviour
{
    public enum DisplayMode { SimpleText, DialogueWindow }

    [Header("Режим")]
    public DisplayMode mode = DisplayMode.SimpleText;

    [Header("Текст")]
    [TextArea(3, 10)] public string signText = "Hello!";
    public string promptText = "[ E ] Read";

    [Header("Для DialogueWindow")]
    public Sprite portrait;
    public bool splitByDoubleNewline = true;

    [Header("Для SimpleText")]
    public int fontSize = 32;
    public Color textColor = Color.yellow;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;
    [Range(0f, 1f)] public float messagePosY = 0.85f;

    [Header("Радиус взаимодействия")]
    public float interactRadius = 3f;

    private bool isReading;
    private GameObject promptObj;
    private GameObject messageObj;
    private Transform player;

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("SignCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

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

    bool PlayerNear()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return false;
            player = p.transform;
        }
        return Vector2.Distance(transform.position, player.position) <= interactRadius;
    }

    void Update()
    {
        bool near = PlayerNear();
        bool dialogActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;
        bool eDown = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (mode == DisplayMode.DialogueWindow)
        {
            if (near && !dialogActive)
            {
                promptObj.SetActive(true);
                if (eDown) { promptObj.SetActive(false); OpenDialog(); }
            }
            else promptObj.SetActive(false);
        }
        else
        {
            if (!near) { promptObj.SetActive(false); if (messageObj != null) messageObj.SetActive(false); isReading = false; return; }

            if (!isReading)
            {
                promptObj.SetActive(true);
                if (messageObj != null) messageObj.SetActive(false);
                if (eDown) { isReading = true; promptObj.SetActive(false); if (messageObj != null) messageObj.SetActive(true); }
            }
            else
            {
                if (eDown) { isReading = false; if (messageObj != null) messageObj.SetActive(false); promptObj.SetActive(true); }
            }
        }
    }

    void OpenDialog()
    {
        string[] lines = splitByDoubleNewline
            ? signText.Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries)
            : new[] { signText };

        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
        DialogueManager.Instance.StartDialogue(portrait, lines, transform, interactRadius + 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}