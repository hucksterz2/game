using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private GameObject panel;
    private bool isOpen = false;
    public bool IsOpen => isOpen;

    void Start()
    {
        if (SettingsManager.Instance == null)
        {
            GameObject go = new GameObject("SettingsManager");
            go.AddComponent<SettingsManager>();
        }
        BuildUI();
    }

    public void OpenSettings() { panel.SetActive(true); isOpen = true; }
    public void CloseSettings() { panel.SetActive(false); isOpen = false; }
    public void ToggleSettings()
    {
        isOpen = !isOpen;
        panel.SetActive(isOpen);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("SettingsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvas.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.92f);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.2f, 0.1f);
        pr.anchorMax = new Vector2(0.8f, 0.9f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        AddLabel(panel.transform, "НАСТРОЙКИ", 0.88f, 0.98f, 60, FontStyle.Bold);

        AddLabel(panel.transform, "Разрешение экрана", 0.78f, 0.84f, 26, FontStyle.Normal);
        GameObject resDropGO = new GameObject("ResDropdown");
        resDropGO.transform.SetParent(panel.transform, false);
        resDropGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        Dropdown resDrop = resDropGO.AddComponent<Dropdown>();
        SetupDropdownRect(resDropGO, 0.71f, 0.78f);
        BuildDropdownTemplate(resDrop);

        resDrop.ClearOptions();
        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        foreach (var r in SettingsManager.AvailableResolutions)
            options.Add($"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz");
        resDrop.AddOptions(options);
        resDrop.value = SettingsManager.CurrentResolutionIndex;
        resDrop.RefreshShownValue();
        resDrop.onValueChanged.AddListener(SettingsManager.SetResolution);

        AddLabel(panel.transform, "Полноэкранный режим", 0.61f, 0.67f, 26, FontStyle.Normal);
        GameObject toggleGO = new GameObject("FullscreenToggle");
        toggleGO.transform.SetParent(panel.transform, false);
        Toggle fsToggle = toggleGO.AddComponent<Toggle>();
        RectTransform tgr = toggleGO.GetComponent<RectTransform>();
        tgr.anchorMin = new Vector2(0.72f, 0.61f);
        tgr.anchorMax = new Vector2(0.80f, 0.67f);
        tgr.offsetMin = tgr.offsetMax = Vector2.zero;

        GameObject bgImg = new GameObject("Background");
        bgImg.transform.SetParent(toggleGO.transform, false);
        Image bgI = bgImg.AddComponent<Image>();
        bgI.color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform bgr = bgImg.GetComponent<RectTransform>();
        bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one;
        bgr.offsetMin = bgr.offsetMax = Vector2.zero;

        GameObject check = new GameObject("Checkmark");
        check.transform.SetParent(bgImg.transform, false);
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.8f, 0.3f);
        RectTransform chr = check.GetComponent<RectTransform>();
        chr.anchorMin = new Vector2(0.15f, 0.15f);
        chr.anchorMax = new Vector2(0.85f, 0.85f);
        chr.offsetMin = chr.offsetMax = Vector2.zero;

        fsToggle.targetGraphic = bgI;
        fsToggle.graphic = checkImg;
        fsToggle.isOn = SettingsManager.IsFullscreen;
        fsToggle.onValueChanged.AddListener(SettingsManager.SetFullscreen);

        AddLabel(panel.transform, "Скорость диалогов", 0.46f, 0.52f, 26, FontStyle.Normal);
        GameObject sliderGO = new GameObject("SpeedSlider");
        sliderGO.transform.SetParent(panel.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        RectTransform slr = sliderGO.GetComponent<RectTransform>();
        slr.anchorMin = new Vector2(0.20f, 0.40f);
        slr.anchorMax = new Vector2(0.80f, 0.45f);
        slr.offsetMin = slr.offsetMax = Vector2.zero;

        GameObject sBg = new GameObject("BG"); sBg.transform.SetParent(sliderGO.transform, false);
        sBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        var sbr = sBg.GetComponent<RectTransform>();
        sbr.anchorMin = Vector2.zero; sbr.anchorMax = Vector2.one;
        sbr.offsetMin = sbr.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var far = fillArea.AddComponent<RectTransform>();
        far.anchorMin = new Vector2(0, 0.25f); far.anchorMax = new Vector2(1, 0.75f);
        far.offsetMin = new Vector2(10, 0); far.offsetMax = new Vector2(-10, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.7f, 1f);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject("HandleArea");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var har = handleArea.AddComponent<RectTransform>();
        har.anchorMin = Vector2.zero; har.anchorMax = Vector2.one;
        har.offsetMin = new Vector2(10, 0); har.offsetMax = new Vector2(-10, 0);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var hr = handle.GetComponent<RectTransform>();
        hr.sizeDelta = new Vector2(20, 0);
        hr.anchorMin = new Vector2(0, 0); hr.anchorMax = new Vector2(0, 1);

        slider.targetGraphic = handleImg;
        slider.fillRect = fillRT;
        slider.handleRect = hr;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0.005f;
        slider.maxValue = 0.10f;
        slider.value = 0.105f - SettingsManager.DialogueSpeed;
        slider.onValueChanged.AddListener(v => SettingsManager.SetDialogueSpeed(0.105f - v));

        AddLabel(panel.transform, "Громкость музыки", 0.28f, 0.34f, 26, FontStyle.Normal);
        GameObject musicSliderGO = new GameObject("MusicSlider");
        musicSliderGO.transform.SetParent(panel.transform, false);
        Slider mSlider = musicSliderGO.AddComponent<Slider>();
        RectTransform msr = musicSliderGO.GetComponent<RectTransform>();
        msr.anchorMin = new Vector2(0.20f, 0.22f);
        msr.anchorMax = new Vector2(0.80f, 0.27f);
        msr.offsetMin = msr.offsetMax = Vector2.zero;

        GameObject mBg = new GameObject("BG"); mBg.transform.SetParent(musicSliderGO.transform, false);
        mBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);
        var mbr = mBg.GetComponent<RectTransform>();
        mbr.anchorMin = Vector2.zero; mbr.anchorMax = Vector2.one;
        mbr.offsetMin = mbr.offsetMax = Vector2.zero;

        GameObject mFillArea = new GameObject("FillArea");
        mFillArea.transform.SetParent(musicSliderGO.transform, false);
        var mfar = mFillArea.AddComponent<RectTransform>();
        mfar.anchorMin = new Vector2(0, 0.25f); mfar.anchorMax = new Vector2(1, 0.75f);
        mfar.offsetMin = new Vector2(10, 0); mfar.offsetMax = new Vector2(-10, 0);

        GameObject mFill = new GameObject("Fill");
        mFill.transform.SetParent(mFillArea.transform, false);
        Image mFillImg = mFill.AddComponent<Image>();
        mFillImg.color = new Color(0.4f, 0.9f, 0.4f);
        var mFillRT = mFill.GetComponent<RectTransform>();
        mFillRT.anchorMin = Vector2.zero; mFillRT.anchorMax = Vector2.one;
        mFillRT.offsetMin = mFillRT.offsetMax = Vector2.zero;

        GameObject mHandleArea = new GameObject("HandleArea");
        mHandleArea.transform.SetParent(musicSliderGO.transform, false);
        var mhar = mHandleArea.AddComponent<RectTransform>();
        mhar.anchorMin = Vector2.zero; mhar.anchorMax = Vector2.one;
        mhar.offsetMin = new Vector2(10, 0); mhar.offsetMax = new Vector2(-10, 0);

        GameObject mHandle = new GameObject("Handle");
        mHandle.transform.SetParent(mHandleArea.transform, false);
        Image mHandleImg = mHandle.AddComponent<Image>();
        mHandleImg.color = Color.white;
        var mhr = mHandle.GetComponent<RectTransform>();
        mhr.sizeDelta = new Vector2(20, 0);
        mhr.anchorMin = new Vector2(0, 0); mhr.anchorMax = new Vector2(0, 1);

        mSlider.targetGraphic = mHandleImg;
        mSlider.fillRect = mFillRT;
        mSlider.handleRect = mhr;
        mSlider.direction = Slider.Direction.LeftToRight;
        mSlider.minValue = 0f;
        mSlider.maxValue = 1f;
        mSlider.value = SettingsManager.MusicVolume;
        mSlider.onValueChanged.AddListener(SettingsManager.SetMusicVolume);

        GameObject closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(panel.transform, false);
        Image cI = closeGO.AddComponent<Image>(); cI.color = new Color(0.3f, 0.3f, 0.4f);
        Button cB = closeGO.AddComponent<Button>();
        cB.onClick.AddListener(CloseSettings);
        RectTransform cR = closeGO.GetComponent<RectTransform>();
        cR.anchorMin = new Vector2(0.35f, 0.05f);
        cR.anchorMax = new Vector2(0.65f, 0.14f);
        cR.offsetMin = cR.offsetMax = Vector2.zero;
        AddLabel(closeGO.transform, "Закрыть", 0, 1, 28, FontStyle.Bold);

        panel.SetActive(false);
    }

    void AddLabel(Transform parent, string txt, float yMin, float yMax, int size, FontStyle style)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.text = txt;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = style;
        t.raycastTarget = false;
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.05f, yMin);
        r.anchorMax = new Vector2(0.95f, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void SetupDropdownRect(GameObject go, float yMin, float yMax)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.20f, yMin);
        r.anchorMax = new Vector2(0.80f, yMax);
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    void BuildDropdownTemplate(Dropdown dd)
    {
        GameObject capGO = new GameObject("CaptionText");
        capGO.transform.SetParent(dd.transform, false);
        Text capText = capGO.AddComponent<Text>();
        capText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        capText.color = Color.white;
        capText.fontSize = 22;
        capText.alignment = TextAnchor.MiddleCenter;
        var capR = capGO.GetComponent<RectTransform>();
        capR.anchorMin = Vector2.zero; capR.anchorMax = Vector2.one;
        capR.offsetMin = new Vector2(10, 0); capR.offsetMax = new Vector2(-25, 0);
        dd.captionText = capText;

        GameObject template = new GameObject("Template");
        template.transform.SetParent(dd.transform, false);
        template.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
        var tr = template.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 0);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0, 0);
        tr.sizeDelta = new Vector2(0, 200);

        ScrollRect sr = template.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var vr = viewport.GetComponent<RectTransform>();
        vr.anchorMin = Vector2.zero; vr.anchorMax = Vector2.one;
        vr.offsetMin = vr.offsetMax = Vector2.zero;
        sr.viewport = vr;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var cr2 = content.AddComponent<RectTransform>();
        cr2.anchorMin = new Vector2(0, 1); cr2.anchorMax = new Vector2(1, 1);
        cr2.pivot = new Vector2(0.5f, 1);
        cr2.sizeDelta = new Vector2(0, 28);
        sr.content = cr2;

        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        Toggle itemToggle = item.AddComponent<Toggle>();
        var ir = item.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 0.5f); ir.anchorMax = new Vector2(1, 0.5f);
        ir.sizeDelta = new Vector2(0, 28);

        GameObject itemBg = new GameObject("Item Background");
        itemBg.transform.SetParent(item.transform, false);
        itemBg.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 0);
        var ibr = itemBg.GetComponent<RectTransform>();
        ibr.anchorMin = Vector2.zero; ibr.anchorMax = Vector2.one;
        ibr.offsetMin = ibr.offsetMax = Vector2.zero;
        itemToggle.targetGraphic = itemBg.GetComponent<Image>();

        GameObject itemLabel = new GameObject("Item Label");
        itemLabel.transform.SetParent(item.transform, false);
        Text labelTxt = itemLabel.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.color = Color.white;
        labelTxt.fontSize = 20;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        var ilr = itemLabel.GetComponent<RectTransform>();
        ilr.anchorMin = Vector2.zero; ilr.anchorMax = Vector2.one;
        ilr.offsetMin = new Vector2(10, 0); ilr.offsetMax = new Vector2(-10, 0);
        dd.itemText = labelTxt;
        dd.template = tr;
        template.SetActive(false);
    }
}