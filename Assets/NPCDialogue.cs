using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CircleCollider2D))]
public class NPCDialogue : MonoBehaviour
{
    public enum Speaker { NPC, Player }

    [System.Serializable]
    public class DialogueEntry
    {
        public Speaker speaker = Speaker.NPC;
        [TextArea(2, 5)] public string text;
    }

    [Header("œŒ–“–≈“€")]
    public Sprite npcPortrait;
    public Sprite playerPortrait;

    [Header("ƒ»¿ÀŒ√ (ÔÓˇ‰ÓÍ Ë ÍÚÓ „Ó‚ÓËÚ)")]
    public DialogueEntry[] dialogue;

    [Header("œŒƒ— ¿« ¿")]
    public string promptText = "[ E ] √Ó‚ÓËÚ¸";
    public int fontSize = 32;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;

    private bool playerInZone = false;
    private GameObject promptObj;

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

        promptObj = MakePrompt(canvas.transform);
        promptObj.SetActive(false);
    }

    GameObject MakePrompt(Transform parent)
    {
        GameObject go = new GameObject("NPCPrompt");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = promptText;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = promptColor;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, promptPosY);
        r.anchorMax = new Vector2(0.95f, promptPosY);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(0, 80);
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
            playerInZone = false;
    }

    void Update()
    {
        bool dialogActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;

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

    void StartDialog()
    {
        if (dialogue == null || dialogue.Length == 0) return;

        DialogueManager.DialogueLine[] lines = new DialogueManager.DialogueLine[dialogue.Length];
        for (int i = 0; i < dialogue.Length; i++)
        {
            lines[i] = new DialogueManager.DialogueLine
            {
                portrait = dialogue[i].speaker == Speaker.NPC ? npcPortrait : playerPortrait,
                text = dialogue[i].text
            };
        }

        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }
        DialogueManager.Instance.StartDialogue(lines);
    }
}