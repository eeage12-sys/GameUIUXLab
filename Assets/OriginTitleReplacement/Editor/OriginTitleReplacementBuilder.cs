#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class OriginTitleReplacementBuilder
{
    private const string BackgroundPath = "Assets/OriginTitleReplacement/Art/OriginTitleBackground.png";

    [MenuItem("Tools/Game UI/ORIGIN UI/12. Replace Title Final (Image-Locked Hover)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("ORIGIN Title", "Play 모드를 먼저 종료해 주세요.", "OK");
            return;
        }

        GameObject uiRoot = GameObject.Find("ORIGIN_UI");
        if (uiRoot == null)
        {
            EditorUtility.DisplayDialog(
                "ORIGIN Title",
                "Hub_Field_Lightweight_V2 씬을 연 상태에서 실행해 주세요.\nORIGIN_UI를 찾지 못했습니다.",
                "OK");
            return;
        }

        OriginUIFlowController flow = uiRoot.GetComponent<OriginUIFlowController>();
        if (flow == null)
            flow = uiRoot.GetComponentInChildren<OriginUIFlowController>(true);

        if (flow == null)
        {
            EditorUtility.DisplayDialog("ORIGIN Title", "OriginUIFlowController를 찾지 못했습니다.", "OK");
            return;
        }

        PrepareBackgroundImporter();

        Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        if (background == null)
        {
            EditorUtility.DisplayDialog(
                "ORIGIN Title",
                "OriginTitleBackground.png를 찾지 못했습니다.\nOriginTitleReplacement 폴더를 Assets 바로 아래에 넣어 주세요.",
                "OK");
            return;
        }

        Transform old = uiRoot.transform.Find("TitleScreen");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        GameObject title = UIObject("TitleScreen", uiRoot.transform);
        Stretch(title.GetComponent<RectTransform>());
        title.transform.SetAsLastSibling();

        CanvasGroup titleGroup = title.AddComponent<CanvasGroup>();
        titleGroup.interactable = true;
        titleGroup.blocksRaycasts = true;

        // IMPORTANT:
        // All menu buttons are children of this image RectTransform.
        // Therefore Free Aspect / 16:9 / 1280x720 cropping can no longer move
        // the hover FX away from the baked menu artwork.
        GameObject bgGo = UIObject("FinalTitleBackground", title.transform);
        Stretch(bgGo.GetComponent<RectTransform>());

        Image bg = bgGo.AddComponent<Image>();
        bg.sprite = background;
        bg.preserveAspect = true;
        bg.raycastTarget = false;
        bg.color = Color.white;

        AspectRatioFitter fitter = bgGo.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1672f / 941f;

        OriginTitleMenuActions actions = title.AddComponent<OriginTitleMenuActions>();
        actions.flow = flow;

        OriginTitleSelectionManager manager = title.AddComponent<OriginTitleSelectionManager>();

        // These are exact normalized rectangles from the 1672×941 source artwork.
        // y values are converted from top-left image pixels to Unity bottom-left anchors.
        Button start = CreateMenuButton(
            "StartGameButton", bgGo.transform, manager, 0,
            new Vector2(0.063995f, 0.515409f),
            new Vector2(0.299043f, 0.579171f),
            true);

        Button cont = CreateMenuButton(
            "ContinueButton", bgGo.transform, manager, 1,
            new Vector2(0.086722f, 0.429330f),
            new Vector2(0.299641f, 0.489904f),
            false);

        Button settings = CreateMenuButton(
            "SettingsButton", bgGo.transform, manager, 2,
            new Vector2(0.086722f, 0.347503f),
            new Vector2(0.299641f, 0.407014f),
            false);

        Button exit = CreateMenuButton(
            "ExitButton", bgGo.transform, manager, 3,
            new Vector2(0.086722f, 0.264612f),
            new Vector2(0.299641f, 0.323061f),
            false);

        start.onClick.AddListener(actions.StartGame);
        cont.onClick.AddListener(actions.ContinueGame);
        settings.onClick.AddListener(actions.OpenSettings);
        exit.onClick.AddListener(actions.ExitGame);

        SetNavigation(start, exit, cont);
        SetNavigation(cont, start, settings);
        SetNavigation(settings, cont, exit);
        SetNavigation(exit, settings, start);

        GameObject settingsPanel = BuildSettingsPanel(title.transform, actions);
        actions.settingsPanel = settingsPanel;
        settingsPanel.SetActive(false);

        Undo.RecordObject(flow, "Bind ORIGIN Final Title");
        flow.titleRoot = title;
        flow.titleFirstButton = start;
        EditorUtility.SetDirty(flow);

        title.SetActive(true);

        EditorSceneManager.MarkSceneDirty(uiRoot.scene);
        EditorSceneManager.SaveScene(uiRoot.scene);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "ORIGIN Title 수정 완료",
            "이번 버전은 선택 UI를 배경 이미지의 자식으로 붙였습니다.\n\n" +
            "그래서 Game View가 Free Aspect여도 이미지가 확대/크롭되는 만큼\n" +
            "버튼과 Hover도 똑같이 따라갑니다.\n\n" +
            "선택 색은 글씨를 가리지 않는 아주 옅은 반투명 파랑 + 테두리만 사용합니다.",
            "OK");
    }

    private static Button CreateMenuButton(
        string name,
        Transform imageParent,
        OriginTitleSelectionManager manager,
        int index,
        Vector2 anchorMin,
        Vector2 anchorMax,
        bool isStart)
    {
        GameObject root = UIObject(name, imageParent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Invisible clickable target.
        Image hit = root.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0.001f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;

        CanvasGroup dimmer = isStart ? BuildStartDimmer(root.transform) : null;

        GameObject selectionFx = BuildSelectionFx(root.transform, isStart);
        selectionFx.SetActive(index == 0);

        OriginTitleButtonSelectionFx selector = root.AddComponent<OriginTitleButtonSelectionFx>();
        selector.manager = manager;
        selector.index = index;

        manager.entries.Add(new OriginTitleSelectionManager.Entry
        {
            button = root,
            selectionFx = selectionFx,
            bakedStartDimmer = dimmer
        });

        return button;
    }

    private static CanvasGroup BuildStartDimmer(Transform parent)
    {
        GameObject go = UIObject("BakedStartDimmer", parent);
        Stretch(go.GetComponent<RectTransform>());

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.01f, 0.025f, 0.055f, 0.58f);
        image.raycastTarget = false;

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        return group;
    }

    private static GameObject BuildSelectionFx(Transform parent, bool isStart)
    {
        GameObject fx = UIObject("SelectionFX", parent);
        RectTransform rt = fx.GetComponent<RectTransform>();
        Stretch(rt);

        // Small inset keeps the FX inside the artwork's decorative frame.
        rt.offsetMin = new Vector2(2f, 3f);
        rt.offsetMax = new Vector2(-2f, -3f);

        // Extremely transparent: text underneath stays fully readable.
        Image fill = fx.AddComponent<Image>();
        fill.color = new Color(0.08f, 0.38f, 0.85f, isStart ? 0.025f : 0.055f);
        fill.raycastTarget = false;

        Outline outline = fx.AddComponent<Outline>();
        outline.effectColor = new Color(0.20f, 0.66f, 1.00f, 0.88f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        // Left gold marker.
        GameObject gold = UIObject("GoldEdge", fx.transform);
        RectTransform grt = gold.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0f, 0.08f);
        grt.anchorMax = new Vector2(0f, 0.92f);
        grt.pivot = new Vector2(0f, 0.5f);
        grt.anchoredPosition = Vector2.zero;
        grt.sizeDelta = new Vector2(3f, 0f);

        Image goldImage = gold.AddComponent<Image>();
        goldImage.color = new Color(0.95f, 0.78f, 0.31f, 0.90f);
        goldImage.raycastTarget = false;

        // Thin top/bottom light.
        CreateLine("TopLight", fx.transform, 1f, new Color(0.30f, 0.73f, 1f, 0.62f));
        CreateLine("BottomLight", fx.transform, 0f, new Color(0.12f, 0.46f, 0.92f, 0.52f));

        return fx;
    }

    private static void CreateLine(string name, Transform parent, float yAnchor, Color color)
    {
        GameObject go = UIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.01f, yAnchor);
        rt.anchorMax = new Vector2(0.99f, yAnchor);
        rt.pivot = new Vector2(0.5f, yAnchor);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 1.2f);

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static GameObject BuildSettingsPanel(Transform parent, OriginTitleMenuActions actions)
    {
        GameObject panel = UIObject("SettingsPanel", parent);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.63f, 0.20f);
        prt.anchorMax = new Vector2(0.94f, 0.78f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.015f, 0.025f, 0.055f, 0.97f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.58f, 0.26f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 28, 28);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateTMP(panel.transform, "Title", "SETTINGS", 28, FontStyles.Bold);
        title.color = new Color(0.93f, 0.82f, 0.58f, 1f);
        AddHeight(title.gameObject, 48);

        TMP_Text resLabel = CreateTMP(panel.transform, "ResolutionLabel", "RESOLUTION", 14, FontStyles.Bold);
        resLabel.color = new Color(0.72f, 0.75f, 0.82f, 1f);
        AddHeight(resLabel.gameObject, 26);

        TMP_Dropdown resolution = CreateDropdown(panel.transform, "ResolutionDropdown");
        AddHeight(resolution.gameObject, 46);

        Toggle fullscreen = CreateToggle(panel.transform, "FullscreenToggle", "FULLSCREEN");
        AddHeight(fullscreen.gameObject, 38);

        TMP_Text qualityLabel = CreateTMP(panel.transform, "QualityLabel", "QUALITY", 14, FontStyles.Bold);
        qualityLabel.color = new Color(0.72f, 0.75f, 0.82f, 1f);
        AddHeight(qualityLabel.gameObject, 26);

        TMP_Dropdown quality = CreateDropdown(panel.transform, "QualityDropdown");
        AddHeight(quality.gameObject, 46);

        GameObject spacer = UIObject("Spacer", panel.transform);
        AddHeight(spacer, 12);

        Button apply = CreateVisibleButton(panel.transform, "ApplyButton", "APPLY");
        AddHeight(apply.gameObject, 46);

        Button close = CreateVisibleButton(panel.transform, "CloseButton", "CLOSE");
        AddHeight(close.gameObject, 42);

        actions.resolutionDropdown = resolution;
        actions.qualityDropdown = quality;
        actions.fullscreenToggle = fullscreen;
        actions.applyButton = apply;
        actions.closeButton = close;

        return panel;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, string name)
    {
        GameObject go = UIObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.06f, 0.09f, 0.16f, 1f);

        TMP_Dropdown dd = go.AddComponent<TMP_Dropdown>();

        TMP_Text label = CreateTMP(go.transform, "Label", "OPTION", 16, FontStyles.Normal);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0.05f, 0f);
        lrt.anchorMax = new Vector2(0.90f, 1f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        dd.captionText = label;
        dd.targetGraphic = image;

        GameObject template = UIObject("Template", go.transform);
        RectTransform trt = template.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -2f);
        trt.sizeDelta = new Vector2(0f, 150f);

        Image templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.025f, 0.04f, 0.075f, 1f);

        ScrollRect sr = template.AddComponent<ScrollRect>();

        GameObject viewport = UIObject("Viewport", template.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;

        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = UIObject("Content", viewport.transform);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0f, 30f);

        Toggle item = CreateToggle(content.transform, "Item", "OPTION");
        RectTransform irt = item.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 1f);
        irt.anchorMax = new Vector2(1f, 1f);
        irt.pivot = new Vector2(0.5f, 1f);
        irt.sizeDelta = new Vector2(0f, 30f);

        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = content.GetComponent<RectTransform>();
        sr.horizontal = false;

        dd.template = trt;
        dd.itemText = item.GetComponentInChildren<TMP_Text>(true);

        template.SetActive(false);
        return dd;
    }

    private static Toggle CreateToggle(Transform parent, string name, string labelText)
    {
        GameObject go = UIObject(name, parent);
        Toggle toggle = go.AddComponent<Toggle>();

        GameObject box = UIObject("Background", go.transform);
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0.5f);
        brt.anchorMax = new Vector2(0f, 0.5f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = new Vector2(4f, 0f);
        brt.sizeDelta = new Vector2(24f, 24f);

        Image boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.08f, 0.11f, 0.18f, 1f);

        GameObject check = UIObject("Checkmark", box.transform);
        Stretch(check.GetComponent<RectTransform>());
        check.GetComponent<RectTransform>().offsetMin = new Vector2(5f, 5f);
        check.GetComponent<RectTransform>().offsetMax = new Vector2(-5f, -5f);

        Image checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.86f, 0.68f, 0.28f, 1f);

        TMP_Text label = CreateTMP(go.transform, "Label", labelText, 15, FontStyles.Bold);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(40f, 0f);
        lrt.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;

        return toggle;
    }

    private static Button CreateVisibleButton(Transform parent, string name, string text)
    {
        GameObject go = UIObject(name, parent);

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.06f, 0.11f, 0.20f, 1f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.06f, 0.11f, 0.20f, 1f);
        colors.highlightedColor = new Color(0.09f, 0.27f, 0.48f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.12f, 0.37f, 0.64f, 1f);
        button.colors = colors;

        TMP_Text label = CreateTMP(go.transform, "Label", text, 16, FontStyles.Bold);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private static TMP_Text CreateTMP(
        Transform parent,
        string name,
        string text,
        float size,
        FontStyles style)
    {
        GameObject go = UIObject(name, parent);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static void AddHeight(GameObject go, float height)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();

        element.preferredHeight = height;
    }

    private static void SetNavigation(Button button, Button up, Button down)
    {
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnUp = up;
        navigation.selectOnDown = down;
        button.navigation = navigation;
    }

    private static void PrepareBackgroundImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
