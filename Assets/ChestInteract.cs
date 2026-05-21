using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cainos.PixelArtPlatformer_VillageProps;

public class ChestInteract : MonoBehaviour
{
    [Header("Если этот сундук даёт ключ")]
    public string setsFlag = "";

    [Header("Подсказки")]
    public string openPrompt = "[ E ] Открыть";
    public string closePrompt = "[ E ] Закрыть";
    public int fontSize = 32;
    public Color promptColor = Color.white;
    [Range(0f, 1f)] public float promptPosY = 0.75f;

    [Header("Поведение")]
    public bool canCloseAgain = true;

    [Header("Лут (необязательно)")]
    [TextArea] public string lootMessage = "";
    public Sprite lootIcon;

    [Header("Радиус")]
    public float interactRadius = 2f;

    private Chest chest;
    private GameObject promptObj;
    private Text promptText;
    private Transform player;

    void Start()
    {
        chest = GetComponent<Chest>();
        if (chest == null)
        {
            Debug.LogError("ChestInteract: на объекте нет компонента Chest!");
            enabled = false;
            return;
        }
        BuildUI();
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("ChestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        promptObj = new GameObject("ChestPrompt");
        promptObj.transform.SetParent(canvas.transform, false);
        promptText = promptObj.AddComponent<Text>();
        promptText.text = openPrompt;
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = fontSize;
        promptText.color = promptColor;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.fontStyle = FontStyle.Bold;
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
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return false;
            player = p.transform.root;
        }
        return Vector2.Distance(transform.position, player.position) <= interactRadius;
    }

    void Update()
    {
        bool near = PlayerNear();
        bool dialogActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive;

        if (near && !dialogActive)
        {
            bool show = !chest.IsOpened || canCloseAgain;
            promptObj.SetActive(show);
            promptText.text = chest.IsOpened ? closePrompt : openPrompt;

            if (show && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (!chest.IsOpened)
                {
                    chest.Open();

                    if (!string.IsNullOrEmpty(setsFlag))
                        GameFlags.Set(setsFlag, true);

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}