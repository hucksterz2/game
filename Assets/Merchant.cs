using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Merchant : MonoBehaviour
{
    [Header("Имя торговца")]
    public string merchantName = "Торговец";
    public Sprite merchantPortrait;

    [Header("Приветствие (диалог перед магазином)")]
    [TextArea(2, 5)] public string[] greetingLines;

    [Header("Ассортимент")]
    public ShopItem[] items;

    [Header("Радиус взаимодействия")]
    public float interactRadius = 2.5f;

    [Header("Подсказка")]
    public string promptText = "[ E ] Поговорить с торговцем";
    public int fontSize = 30;
    [Range(0f, 1f)] public float promptPosY = 0.75f;

    private GameObject promptObj;
    private Transform player;
    private bool busy = false;
    private ShopMenu shopMenu;

    void Start()
    {
        BuildPrompt();

        shopMenu = FindFirstObjectByType<ShopMenu>();
        if (shopMenu == null)
        {
            GameObject sm = new GameObject("ShopMenu");
            shopMenu = sm.AddComponent<ShopMenu>();
        }
    }

    void BuildPrompt()
    {
        GameObject canvasObj = new GameObject("MerchantCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        promptObj = new GameObject("MerchantPrompt");
        promptObj.transform.SetParent(canvas.transform, false);
        Text t = promptObj.AddComponent<Text>();
        t.text = promptText;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;
        RectTransform r = promptObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, promptPosY);
        r.anchorMax = new Vector2(0.95f, promptPosY);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(0, 60);
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
        bool shopActive = shopMenu != null && shopMenu.IsOpen;

        if (near && !dialogActive && !shopActive && !busy)
        {
            promptObj.SetActive(true);
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                promptObj.SetActive(false);
                busy = true;
                StartCoroutine(DialogThenShop());
            }
        }
        else promptObj.SetActive(false);
    }

    IEnumerator DialogThenShop()
    {
        if (DialogueManager.Instance == null)
        {
            GameObject go = new GameObject("DialogueManager");
            go.AddComponent<DialogueManager>();
        }

        if (greetingLines != null && greetingLines.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(merchantPortrait, greetingLines);
            yield return null;
            while (DialogueManager.Instance.IsActive)
                yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        shopMenu.OpenShop(merchantName, items);
        busy = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}