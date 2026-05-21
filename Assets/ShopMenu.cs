using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour
{
    private GameObject panel;
    private GameObject overlay;
    private Transform itemListContainer;
    private Text coinText;
    private Text titleText;
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    void Start()
    {
        BuildUI();
    }

    public void OpenShop(string merchantName, ShopItem[] items)
    {
        if (overlay != null) overlay.SetActive(true);
        panel.SetActive(true);
        isOpen = true;
        Time.timeScale = 0f;

        if (titleText != null) titleText.text = merchantName.ToUpper();

        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        if (items != null)
            foreach (var item in items)
                CreateItemRow(item);

        UpdateCoinText();
    }

    public void CloseShop()
    {
        if (overlay != null) overlay.SetActive(false);
        panel.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!isOpen) return;
    }

    void UpdateCoinText()
    {
        if (coinText != null) coinText.text = "Монет у вас: " + CoinManager.Coins;
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("ShopCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3500;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvas.transform, false);
        Image ov = overlay.AddComponent<Image>();
        ov.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform or_ = overlay.GetComponent<RectTransform>();
        or_.anchorMin = Vector2.zero; or_.anchorMax = Vector2.one;
        or_.offsetMin = or_.offsetMax = Vector2.zero;

        panel = new GameObject("ShopPanel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.10f, 0.07f, 0.05f, 0.98f);
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.7f, 0.5f, 0.2f);
        panelOutline.effectDistance = new Vector2(4, -4);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.18f, 0.10f);
        pr.anchorMax = new Vector2(0.82f, 0.92f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.30f, 0.18f, 0.08f, 1f);
        RectTransform hr = header.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(0, 0.88f);
        hr.anchorMax = new Vector2(1, 1f);
        hr.offsetMin = hr.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.text = "ТОРГОВЕЦ";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = new Color(1f, 0.85f, 0.4f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        titleText.raycastTarget = false;
        RectTransform titleR = titleGO.GetComponent<RectTransform>();
        titleR.anchorMin = Vector2.zero; titleR.anchorMax = Vector2.one;
        titleR.offsetMin = titleR.offsetMax = Vector2.zero;

        GameObject coinPanel = new GameObject("CoinPanel");
        coinPanel.transform.SetParent(panel.transform, false);
        Image coinBg = coinPanel.AddComponent<Image>();
        coinBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        RectTransform cpr = coinPanel.GetComponent<RectTransform>();
        cpr.anchorMin = new Vector2(0.05f, 0.78f);
        cpr.anchorMax = new Vector2(0.95f, 0.86f);
        cpr.offsetMin = cpr.offsetMax = Vector2.zero;

        GameObject coinDisplay = new GameObject("CoinDisplay");
        coinDisplay.transform.SetParent(coinPanel.transform, false);
        coinText = coinDisplay.AddComponent<Text>();
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinText.fontSize = 30;
        coinText.color = new Color(1f, 0.85f, 0.2f);
        coinText.alignment = TextAnchor.MiddleCenter;
        coinText.fontStyle = FontStyle.Bold;
        coinText.raycastTarget = false;
        coinText.text = "Монет у вас: 0";
        RectTransform cdr = coinDisplay.GetComponent<RectTransform>();
        cdr.anchorMin = Vector2.zero; cdr.anchorMax = Vector2.one;
        cdr.offsetMin = cdr.offsetMax = Vector2.zero;

        GameObject scrollGO = new GameObject("ItemList");
        scrollGO.transform.SetParent(panel.transform, false);
        RectTransform sr = scrollGO.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.04f, 0.14f);
        sr.anchorMax = new Vector2(0.96f, 0.76f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.04f, 0.03f, 0.8f);

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGO.transform, false);
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.1f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform vr = viewport.GetComponent<RectTransform>();
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
        vr.offsetMin = new Vector2(8, 8); vr.offsetMax = new Vector2(-8, -8);
        scroll.viewport = vr;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentR = content.AddComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0, 1);
        contentR.anchorMax = new Vector2(1, 1);
        contentR.pivot = new Vector2(0.5f, 1);
        contentR.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentR;

        itemListContainer = content.transform;

        GameObject closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(panel.transform, false);
        Image cI = closeGO.AddComponent<Image>();
        cI.color = new Color(0.55f, 0.18f, 0.18f);
        Button cB = closeGO.AddComponent<Button>();
        cB.targetGraphic = cI;
        var ccolors = cB.colors;
        ccolors.highlightedColor = new Color(0.75f, 0.25f, 0.25f);
        ccolors.pressedColor = new Color(0.40f, 0.12f, 0.12f);
        cB.colors = ccolors;
        cB.onClick.AddListener(CloseShop);
        RectTransform cR = closeGO.GetComponent<RectTransform>();
        cR.anchorMin = new Vector2(0.35f, 0.03f);
        cR.anchorMax = new Vector2(0.65f, 0.11f);
        cR.offsetMin = cR.offsetMax = Vector2.zero;

        GameObject closeTxt = new GameObject("CloseText");
        closeTxt.transform.SetParent(closeGO.transform, false);
        Text ct = closeTxt.AddComponent<Text>();
        ct.text = "ЗАКРЫТЬ";
        ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ct.fontSize = 28;
        ct.color = Color.white;
        ct.alignment = TextAnchor.MiddleCenter;
        ct.fontStyle = FontStyle.Bold;
        ct.raycastTarget = false;
        RectTransform ctr = closeTxt.GetComponent<RectTransform>();
        ctr.anchorMin = Vector2.zero; ctr.anchorMax = Vector2.one;
        ctr.offsetMin = ctr.offsetMax = Vector2.zero;

        overlay.SetActive(false);
        panel.SetActive(false);
    }

    void CreateItemRow(ShopItem item)
    {
        GameObject row = new GameObject("Item_" + item.itemName);
        row.transform.SetParent(itemListContainer, false);
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.18f, 0.14f, 0.10f, 1f);
        Outline rowOutline = row.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.4f, 0.3f, 0.15f);
        rowOutline.effectDistance = new Vector2(2, -2);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 110;

        float textStartX = 0.03f;
        if (item.icon != null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(row.transform, false);
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            RectTransform ir = iconGO.GetComponent<RectTransform>();
            ir.anchorMin = new Vector2(0.01f, 0.1f);
            ir.anchorMax = new Vector2(0.13f, 0.9f);
            ir.offsetMin = ir.offsetMax = Vector2.zero;
            textStartX = 0.15f;
        }

        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(row.transform, false);
        Text nameT = nameGO.AddComponent<Text>();
        nameT.text = item.itemName;
        nameT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameT.fontSize = 26;
        nameT.color = new Color(1f, 0.95f, 0.7f);
        nameT.fontStyle = FontStyle.Bold;
        nameT.alignment = TextAnchor.UpperLeft;
        nameT.raycastTarget = false;
        RectTransform nr = nameGO.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(textStartX, 0.55f);
        nr.anchorMax = new Vector2(0.65f, 0.95f);
        nr.offsetMin = new Vector2(8, 0); nr.offsetMax = Vector2.zero;

        GameObject descGO = new GameObject("Desc");
        descGO.transform.SetParent(row.transform, false);
        Text descT = descGO.AddComponent<Text>();
        descT.text = item.description;
        descT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descT.fontSize = 18;
        descT.color = new Color(0.85f, 0.80f, 0.70f);
        descT.alignment = TextAnchor.UpperLeft;
        descT.horizontalOverflow = HorizontalWrapMode.Wrap;
        descT.raycastTarget = false;
        RectTransform dr = descGO.GetComponent<RectTransform>();
        dr.anchorMin = new Vector2(textStartX, 0.05f);
        dr.anchorMax = new Vector2(0.65f, 0.55f);
        dr.offsetMin = new Vector2(8, 5); dr.offsetMax = Vector2.zero;

        GameObject btnGO = new GameObject("BuyButton");
        btnGO.transform.SetParent(row.transform, false);
        Image btnI = btnGO.AddComponent<Image>();
        btnI.color = new Color(0.20f, 0.50f, 0.25f);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnI;
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.30f, 0.65f, 0.32f);
        bc.pressedColor = new Color(0.13f, 0.35f, 0.18f);
        btn.colors = bc;
        ShopItem itemRef = item;
        btn.onClick.AddListener(() => TryBuy(itemRef));
        RectTransform br = btnGO.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0.68f, 0.12f);
        br.anchorMax = new Vector2(0.98f, 0.88f);
        br.offsetMin = br.offsetMax = Vector2.zero;

        GameObject btnTxt = new GameObject("BuyText");
        btnTxt.transform.SetParent(btnGO.transform, false);
        Text bt = btnTxt.AddComponent<Text>();
        bt.text = "КУПИТЬ\n" + item.price + " монет";
        bt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bt.fontSize = 22;
        bt.color = Color.white;
        bt.alignment = TextAnchor.MiddleCenter;
        bt.fontStyle = FontStyle.Bold;
        bt.raycastTarget = false;
        RectTransform btr = btnTxt.GetComponent<RectTransform>();
        btr.anchorMin = Vector2.zero; btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
    }

    void TryBuy(ShopItem item)
    {
        if (!CoinManager.TrySpend(item.price)) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) { Debug.LogWarning("Player not found!"); return; }
        Transform player = playerObj.transform.root;

        switch (item.effect)
        {
            case ShopItem.EffectType.IncreaseMaxHP:
                PlayerHealth ph = player.GetComponentInChildren<PlayerHealth>();
                if (ph == null) ph = playerObj.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.maxHP += item.amount;
                    ph.currentHP += item.amount;
                    Debug.Log("MaxHP +" + item.amount + " (now " + ph.maxHP + ")");
                }
                else Debug.LogWarning("PlayerHealth not found!");
                break;

            case ShopItem.EffectType.SpeedBoost:
                PlayerController pc = player.GetComponentInChildren<PlayerController>();
                if (pc == null) pc = playerObj.GetComponent<PlayerController>();
                if (pc != null)
                {
                    float oldSpeed = pc.speed;
                    pc.speed += item.amount;
                    Debug.Log("Speed: " + oldSpeed + " -> " + pc.speed);
                }
                else Debug.LogWarning("PlayerController not found!");
                break;

            case ShopItem.EffectType.SetFlag:
                if (!string.IsNullOrEmpty(item.flagName))
                {
                    GameFlags.Set(item.flagName, true);
                    Debug.Log("Flag set: " + item.flagName);
                }
                else Debug.LogWarning("FlagName is empty!");
                break;
        }

        UpdateCoinText();
    }
}