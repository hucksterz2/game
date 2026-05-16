using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cainos.PixelArtPlatformer_VillageProps;

[RequireComponent(typeof(CircleCollider2D))]
public class ChestInteract : MonoBehaviour
{
    [Header("ПОДСКАЗКА")]
    public string openPrompt = "[ E ] Открыть";
    public string closePrompt = "[ E ] Закрыть";
    public int fontSize = 32;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;

    [Header("МОЖНО ЗАКРЫВАТЬ?")]
    public bool canCloseAgain = true;

    [Header("СОДЕРЖИМОЕ (необязательно)")]
    [TextArea] public string lootMessage = "";
    public Sprite lootIcon;

    private Chest chest;
    private bool playerInZone = false;
    private GameObject promptObj;
    private Text promptText;

    void Start()
    {
        chest = GetComponent<Chest>();
        if (chest == null)
        {
            Debug.LogError("Нет компонента Chest на " + gameObject.name);
            enabled = false;
            return;
        }

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
        GameObject go = new GameObject("ChestPrompt");
        go.transform.SetParent(parent, false);
        promptText = go.AddComponent<Text>();
        promptText.text = openPrompt;
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = fontSize;
        promptText.color = promptColor;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.fontStyle = FontStyle.Bold;
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
            bool showPrompt = !chest.IsOpened || canCloseAgain;
            promptObj.SetActive(showPrompt);
            promptText.text = chest.IsOpened ? closePrompt : openPrompt;

            if (showPrompt && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (!chest.IsOpened)
                {
                    chest.Open();
                    if (!string.IsNullOrEmpty(lootMessage))
                    {
                        promptObj.SetActive(false);
                        if (DialogueManager.Instance == null)
                        {
                            GameObject go = new GameObject("DialogueManager");
                            go.AddComponent<DialogueManager>();
                        }
                        DialogueManager.Instance.StartDialogue(lootIcon, new string[] { lootMessage });
                    }
                }
                else if (canCloseAgain)
                {
                    chest.Close();
                }
            }
        }
        else promptObj.SetActive(false);
    }
}