using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class NPCDialogue : MonoBehaviour
{
    public enum Speaker { NPC, Player }

    [System.Serializable]
    public class Entry
    {
        public Speaker speaker = Speaker.NPC;
        [TextArea(2, 5)] public string text;
    }

    [Header("Портреты")]
    public Sprite npcPortrait;
    public Sprite playerPortrait;

    [Header("Диалог")]
    public Entry[] dialogue;

    [Header("Подсказка")]
    public string promptText = "[ E ] Говорить";
    public int fontSize = 32;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;

    [Header("Радиус")]
    public float interactRadius = 3f;

    private GameObject promptObj;
    private Transform player;

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("NPCCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        promptObj = new GameObject("NPCPrompt");
        promptObj.transform.SetParent(canvas.transform, false);
        Text t = promptObj.AddComponent<Text>();
        t.text = promptText;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = promptColor;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        RectTransform r = promptObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, promptPosY);
        r.anchorMax = new Vector2(0.95f, promptPosY);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(0, 80);
        r.anchoredPosition = Vector2.zero;
        promptObj.SetActive(false);
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

        if (near && !dialogActive)
        {
            promptObj.SetActive(true);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                promptObj.SetActive(false);
                OpenDialog();
            }
        }
        else promptObj.SetActive(false);
    }

    void OpenDialog()
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}