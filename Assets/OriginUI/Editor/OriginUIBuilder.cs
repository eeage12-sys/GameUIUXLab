#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class OriginUIBuilder
{
    private const string RootName = "ORIGIN_UI";
    private const string PrefabFolder = "Assets/UI/Prefabs";

    private static readonly Color White = new Color32(244, 245, 239, 255);
    private static readonly Color Muted = new Color32(166, 173, 180, 255);
    private static readonly Color Gold = new Color32(215, 164, 72, 255);
    private static readonly Color Pink = new Color32(207, 69, 118, 255);
    private static readonly Color Cyan = new Color32(78, 196, 207, 255);
    private static readonly Color Green = new Color32(85, 211, 122, 255);
    private static readonly Color Dark = new Color32(12, 16, 22, 235);

    [MenuItem("Tools/Game UI/ORIGIN UI/1. Build Complete HUD + Menus")]
    public static void BuildAll()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "ORIGIN UI",
                "ORIGIN_UI already exists in this scene. Rebuild it?\n(The existing ORIGIN_UI object will be deleted.)",
                "Rebuild", "Cancel");
            if (!rebuild) return;
            UnityEngine.Object.DestroyImmediate(existing);
        }

        ConfigureSprites();
        EnsureEventSystem();
        EnsureFolder("Assets/UI");
        EnsureFolder(PrefabFolder);

        Sprite panelDark = LoadSprite("panel_dark.png");
        Sprite panelGold = LoadSprite("panel_gold.png");
        Sprite buttonSprite = LoadSprite("button.png");
        Sprite keycap = LoadSprite("keycap.png");
        Sprite gaugeBg = LoadSprite("gauge_bg.png");
        Sprite gaugeFill = LoadSprite("gauge_fill.png");
        Sprite minimapFrame = LoadSprite("minimap_frame.png");
        Sprite iconCompass = LoadSprite("icon_compass.png");
        Sprite iconQuest = LoadSprite("icon_quest.png");
        Sprite iconAttack = LoadSprite("icon_attack.png");
        Sprite iconGuard = LoadSprite("icon_guard.png");
        Sprite iconDodge = LoadSprite("icon_dodge.png");
        Sprite iconJump = LoadSprite("icon_jump.png");
        Sprite iconSuccess = LoadSprite("icon_success.png");

        // Canvas root
        GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Build ORIGIN UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        OriginUIState state = root.AddComponent<OriginUIState>();
        OriginHUDController hudController = root.AddComponent<OriginHUDController>();
        OriginUISceneContext sceneContext = root.AddComponent<OriginUISceneContext>();
        OriginUIFlowController flow = root.AddComponent<OriginUIFlowController>();
        OriginToastController toastController = root.AddComponent<OriginToastController>();
        OriginUIDemoKeys demo = root.AddComponent<OriginUIDemoKeys>();

        GameObject hudRoot = RectGO("HUD", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        // ---- TOP LEFT / MINI MAP ----
        GameObject miniMap = RectGO("MiniMap", hudRoot.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -36), new Vector2(156, 156), new Vector2(0, 1));
        AddImage(miniMap, minimapFrame, Color.white, Image.Type.Simple);
        GameObject compass = RectGO("Compass", miniMap.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(62, 62), new Vector2(.5f, .5f));
        AddImage(compass, iconCompass, Color.white, Image.Type.Simple);
        TMP_Text region = AddText("Region", miniMap.transform, "FIELD  /  VILLAGE", 18, FontStyles.Bold, White, TextAlignmentOptions.Center);
        SetRect(region.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, -20), new Vector2(210, 30), new Vector2(.5f, 1));

        // ---- OBJECTIVE ----
        GameObject objective = RectGO("ObjectivePanel", hudRoot.transform, Vector2.one, Vector2.one, new Vector2(-42, -54), new Vector2(420, 142), Vector2.one);
        AddImage(objective, panelGold, Color.white, Image.Type.Sliced);
        GameObject qIcon = RectGO("QuestIcon", objective.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, 0), new Vector2(70, 70), new Vector2(0, .5f));
        AddImage(qIcon, iconQuest, Color.white, Image.Type.Simple);
        TMP_Text objTitle = AddText("Title", objective.transform, "CURRENT OBJECTIVE", 17, FontStyles.Bold, Gold, TextAlignmentOptions.Left);
        SetRect(objTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(96, -22), new Vector2(-118, 28), new Vector2(0, 1));
        TMP_Text objBody = AddText("Body", objective.transform, "Talk to the village resident", 24, FontStyles.Bold, White, TextAlignmentOptions.Left);
        SetRect(objBody.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(96, 2), new Vector2(-118, 52), new Vector2(0, .5f));
        TMP_Text objDistance = AddText("Distance", objective.transform, "138m", 20, FontStyles.Bold, Gold, TextAlignmentOptions.Right);
        SetRect(objDistance.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-18, -36), new Vector2(100, 30), new Vector2(1, .5f));

        GameObject dungeonInfo = RectGO("DungeonInfo", hudRoot.transform, Vector2.one, Vector2.one, new Vector2(-42, -210), new Vector2(420, 76), Vector2.one);
        AddImage(dungeonInfo, panelDark, new Color(1,1,1,.93f), Image.Type.Sliced);
        TMP_Text timer = AddText("Timer", dungeonInfo.transform, "TIME  03:00", 18, FontStyles.Bold, White, TextAlignmentOptions.Left);
        SetRect(timer.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, 18), new Vector2(135, 28), new Vector2(0, .5f));
        TMP_Text hunt = AddText("Hunt", dungeonInfo.transform, "HUNT  0 / 5", 18, FontStyles.Bold, White, TextAlignmentOptions.Right);
        SetRect(hunt.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-22, 0), new Vector2(190, 30), new Vector2(1, .5f));
        dungeonInfo.SetActive(false);

        // ---- PLAYER HP ----
        GameObject playerStatus = RectGO("PlayerStatus", hudRoot.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 24), new Vector2(560, 86), new Vector2(.5f, 0));
        TMP_Text level = AddText("Level", playerStatus.transform, "Lv. 1", 17, FontStyles.Bold, Muted, TextAlignmentOptions.Left);
        SetRect(level.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, -10), new Vector2(90, 26), new Vector2(0, 1));
        TMP_Text hpText = AddText("HPText", playerStatus.transform, "3456 / 3456", 17, FontStyles.Bold, White, TextAlignmentOptions.Right);
        SetRect(hpText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -10), new Vector2(180, 26), new Vector2(1, 1));
        GameObject hpBar = RectGO("HPBar", playerStatus.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(540, 34), new Vector2(.5f, 0));
        AddImage(hpBar, gaugeBg, Color.white, Image.Type.Sliced);
        GameObject hpFillGo = RectGO("Fill", hpBar.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f));
        Stretch((RectTransform)hpFillGo.transform, 4);
        Image hpFill = AddImage(hpFillGo, gaugeFill, Color.white, Image.Type.Filled);
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        hpFill.fillAmount = 1f;

        // ---- INTERACTION ----
        GameObject interaction = RectGO("InteractionPrompt", hudRoot.transform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 126), new Vector2(320, 56), new Vector2(.5f, 0));
        AddImage(interaction, panelDark, Color.white, Image.Type.Sliced);
        GameObject interactionKey = RectGO("Key", interaction.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, 0), new Vector2(42, 42), new Vector2(0, .5f));
        AddImage(interactionKey, keycap, Color.white, Image.Type.Sliced);
        TMP_Text interactionKeyText = AddText("KeyText", interactionKey.transform, "E", 19, FontStyles.Bold, White, TextAlignmentOptions.Center);
        Stretch(interactionKeyText.rectTransform, 0);
        TMP_Text interactionBody = AddText("Body", interaction.transform, "Interact", 20, FontStyles.Bold, White, TextAlignmentOptions.Left);
        SetRect(interactionBody.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(68, 0), new Vector2(-82, 40), new Vector2(0, .5f));
        interaction.SetActive(false);

        // ---- ACTION GUIDE ----
        GameObject actionGuide = RectGO("ActionGuide", hudRoot.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 34), new Vector2(390, 190), new Vector2(1, 0));
        CreateAction(actionGuide.transform, "Attack", iconAttack, "LMB", "ATTACK", new Vector2(-72, 82), 86, Pink);
        CreateAction(actionGuide.transform, "Guard", iconGuard, "RMB", "GUARD / PARRY", new Vector2(-164, 35), 70, Cyan);
        CreateAction(actionGuide.transform, "Dodge", iconDodge, "Ctrl", "DODGE", new Vector2(-72, -20), 70, Gold);
        CreateAction(actionGuide.transform, "Jump", iconJump, "Space", "JUMP", new Vector2(20, 35), 70, Cyan);

        // ---- TOAST ----
        GameObject toast = RectGO("ToastMessage", root.transform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -92), new Vector2(470, 66), new Vector2(.5f, 1));
        AddImage(toast, panelGold, Color.white, Image.Type.Sliced);
        CanvasGroup toastGroup = toast.AddComponent<CanvasGroup>();
        GameObject successIcon = RectGO("Icon", toast.transform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, 0), new Vector2(48, 48), new Vector2(0, .5f));
        AddImage(successIcon, iconSuccess, Color.white, Image.Type.Simple);
        TMP_Text toastText = AddText("Message", toast.transform, "OBJECTIVE UPDATED", 20, FontStyles.Bold, White, TextAlignmentOptions.Left);
        SetRect(toastText.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(74, 0), new Vector2(-92, 42), new Vector2(0, .5f));
        toast.SetActive(false);

        // ---- MENUS ----
        GameObject titleRoot = CreateFullScreenMenu(root.transform, "TitleScreen", panelDark, "ORIGIN", "ACTION RPG PROTOTYPE");
        Button startButton = CreateMenuButton(titleRoot.transform, "StartButton", "START", buttonSprite, new Vector2(0, -40));
        Button quitTitleButton = CreateMenuButton(titleRoot.transform, "QuitButton", "QUIT", buttonSprite, new Vector2(0, -112));

        GameObject pauseRoot = CreateFullScreenMenu(root.transform, "PauseMenu", panelDark, "PAUSED", "ESC  /  CANCEL");
        Button resumeButton = CreateMenuButton(pauseRoot.transform, "ResumeButton", "CONTINUE", buttonSprite, new Vector2(0, -20));
        Button restartPauseButton = CreateMenuButton(pauseRoot.transform, "RestartButton", "RESTART", buttonSprite, new Vector2(0, -92));
        Button titlePauseButton = CreateMenuButton(pauseRoot.transform, "TitleButton", "BACK TO TITLE", buttonSprite, new Vector2(0, -164));

        GameObject resultRoot = CreateFullScreenMenu(root.transform, "ResultMenu", panelGold, "DUNGEON CLEAR", "HUNT COMPLETE");
        Button restartResultButton = CreateMenuButton(resultRoot.transform, "RestartButton", "RETRY", buttonSprite, new Vector2(0, -40));
        Button titleResultButton = CreateMenuButton(resultRoot.transform, "TitleButton", "BACK TO TITLE", buttonSprite, new Vector2(0, -112));

        GameObject gameOverRoot = CreateFullScreenMenu(root.transform, "GameOverMenu", panelDark, "MISSION FAILED", "PARTY DEFEATED");
        Button retryGameOverButton = CreateMenuButton(gameOverRoot.transform, "RetryButton", "RETRY", buttonSprite, new Vector2(0, -40));
        Button titleGameOverButton = CreateMenuButton(gameOverRoot.transform, "TitleButton", "BACK TO TITLE", buttonSprite, new Vector2(0, -112));

        // Default play mode starts with HUD visible, menus hidden.
        titleRoot.SetActive(false);
        pauseRoot.SetActive(false);
        resultRoot.SetActive(false);
        gameOverRoot.SetActive(false);

        // Wire controllers
        hudController.state = state;
        hudController.regionText = region;
        hudController.hpFill = hpFill;
        hudController.hpText = hpText;
        hudController.levelText = level;
        hudController.objectiveTitleText = objTitle;
        hudController.objectiveBodyText = objBody;
        hudController.objectiveDistanceText = objDistance;
        hudController.dungeonInfoRoot = dungeonInfo;
        hudController.timerText = timer;
        hudController.enemyCountText = hunt;
        hudController.interactionRoot = interaction;
        hudController.interactionKeyText = interactionKeyText;
        hudController.interactionBodyText = interactionBody;

        toastController.toastRoot = toast;
        toastController.canvasGroup = toastGroup;
        toastController.messageText = toastText;

        flow.hudRoot = hudRoot;
        flow.titleRoot = titleRoot;
        flow.pauseRoot = pauseRoot;
        flow.resultRoot = resultRoot;
        flow.gameOverRoot = gameOverRoot;
        flow.pauseFirstButton = resumeButton;
        flow.resultFirstButton = restartResultButton;
        flow.gameOverFirstButton = retryGameOverButton;
        flow.titleFirstButton = startButton;

        sceneContext.state = state;
        demo.state = state;
        demo.toast = toastController;

        UnityEventTools.AddPersistentListener(startButton.onClick, flow.StartFromTitle);
        UnityEventTools.AddPersistentListener(quitTitleButton.onClick, flow.QuitGame);
        UnityEventTools.AddPersistentListener(resumeButton.onClick, flow.Resume);
        UnityEventTools.AddPersistentListener(restartPauseButton.onClick, flow.RestartScene);
        UnityEventTools.AddPersistentListener(titlePauseButton.onClick, flow.ReturnToTitle);
        UnityEventTools.AddPersistentListener(restartResultButton.onClick, flow.RestartScene);
        UnityEventTools.AddPersistentListener(titleResultButton.onClick, flow.ReturnToTitle);
        UnityEventTools.AddPersistentListener(retryGameOverButton.onClick, flow.RestartScene);
        UnityEventTools.AddPersistentListener(titleGameOverButton.onClick, flow.ReturnToTitle);

        SavePrefab(resumeButton.gameObject, PrefabFolder + "/MenuButton.prefab");
        SavePrefab(hpBar, PrefabFolder + "/GaugeBar.prefab");
        SavePrefab(toast, PrefabFolder + "/ToastMessage.prefab");

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("ORIGIN UI", "HUD + menus created.\n\nScene rule:\nHub = DungeonInfo hidden\nNGF_CompactDungeon = TIME + HUNT visible\n\nPlay test:\nF6 Damage / F7 Heal / F8 Hunt / F9 Toast / F10 Interaction / ESC Pause", "OK");
    }

    [MenuItem("Tools/Game UI/ORIGIN UI/2. Show Title Screen In Editor")]
    public static void ShowTitleInEditor()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null) { Debug.LogWarning("Build ORIGIN UI first."); return; }
        Transform title = root.transform.Find("TitleScreen");
        Transform hud = root.transform.Find("HUD");
        if (title != null) title.gameObject.SetActive(true);
        if (hud != null) hud.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Game UI/ORIGIN UI/3. Show HUD In Editor")]
    public static void ShowHUDInEditor()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null) { Debug.LogWarning("Build ORIGIN UI first."); return; }
        Transform title = root.transform.Find("TitleScreen");
        Transform hud = root.transform.Find("HUD");
        if (title != null) title.gameObject.SetActive(false);
        if (hud != null) hud.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Game UI/ORIGIN UI/4. Preview Pause Menu")]
    public static void PreviewPauseMenu()
    {
        PreviewMenu("PauseMenu", true);
    }

    [MenuItem("Tools/Game UI/ORIGIN UI/5. Preview Result Menu")]
    public static void PreviewResultMenu()
    {
        PreviewMenu("ResultMenu", false);
    }

    [MenuItem("Tools/Game UI/ORIGIN UI/6. Preview Game Over Menu")]
    public static void PreviewGameOverMenu()
    {
        PreviewMenu("GameOverMenu", false);
    }

    private static void PreviewMenu(string menuName, bool keepHud)
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null) { Debug.LogWarning("Build ORIGIN UI first."); return; }

        string[] menus = { "TitleScreen", "PauseMenu", "ResultMenu", "GameOverMenu" };
        foreach (string n in menus)
        {
            Transform t = root.transform.Find(n);
            if (t != null) t.gameObject.SetActive(n == menuName);
        }

        Transform hud = root.transform.Find("HUD");
        if (hud != null) hud.gameObject.SetActive(keepHud);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (es != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static GameObject CreateFullScreenMenu(Transform parent, string name, Sprite panelSprite, string title, string subtitle)
    {
        GameObject root = RectGO(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        Image blocker = root.AddComponent<Image>();
        blocker.color = new Color(0.02f, 0.025f, 0.035f, 0.88f);

        GameObject card = RectGO("MenuCard", root.transform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(540, 620), new Vector2(.5f,.5f));
        AddImage(card, panelSprite, Color.white, Image.Type.Sliced);
        TMP_Text titleText = AddText("Title", card.transform, title, 52, FontStyles.Bold, White, TextAlignmentOptions.Center);
        SetRect(titleText.rectTransform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-74), new Vector2(460,72), new Vector2(.5f,1));
        TMP_Text sub = AddText("Subtitle", card.transform, subtitle, 16, FontStyles.Bold, Gold, TextAlignmentOptions.Center);
        SetRect(sub.rectTransform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-145), new Vector2(420,30), new Vector2(.5f,1));
        TMP_Text hint = AddText("InputHint", card.transform, "Mouse  •  Arrow Keys  •  Enter  •  Esc", 15, FontStyles.Normal, Muted, TextAlignmentOptions.Center);
        SetRect(hint.rectTransform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,30), new Vector2(450,30), new Vector2(.5f,0));
        return root;
    }

    private static Button CreateMenuButton(Transform menuRoot, string name, string label, Sprite sprite, Vector2 cardAnchoredPosition)
    {
        Transform card = menuRoot.Find("MenuCard");
        Transform parent = card != null ? card : menuRoot;
        GameObject go = RectGO(name, parent, new Vector2(.5f,.5f), new Vector2(.5f,.5f), cardAnchoredPosition, new Vector2(340, 58), new Vector2(.5f,.5f));
        Image image = AddImage(go, sprite, Color.white, Image.Type.Sliced);
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .88f, .94f, 1f);
        colors.pressedColor = new Color(1f, .84f, .55f, 1f);
        colors.selectedColor = new Color(1f, .88f, .94f, 1f);
        colors.disabledColor = new Color(.45f,.45f,.45f,.65f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        go.AddComponent<OriginUIButtonFeedback>();
        TMP_Text text = AddText("Label", go.transform, label, 20, FontStyles.Bold, White, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 8);
        return button;
    }

    private static void CreateAction(Transform parent, string name, Sprite icon, string key, string label, Vector2 pos, float iconSize, Color labelColor)
    {
        GameObject root = RectGO(name, parent, new Vector2(1,0), new Vector2(1,0), pos, new Vector2(126, 126), new Vector2(1,0));
        GameObject iconGo = RectGO("Icon", root.transform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,0), new Vector2(iconSize,iconSize), new Vector2(.5f,1));
        AddImage(iconGo, icon, Color.white, Image.Type.Simple);
        GameObject keyGo = RectGO("Key", root.transform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,28), new Vector2(Mathf.Max(46, key.Length*16+24), 32), new Vector2(.5f,0));
        AddImage(keyGo, LoadSprite("keycap.png"), Color.white, Image.Type.Sliced);
        TMP_Text keyText = AddText("KeyText", keyGo.transform, key, 15, FontStyles.Bold, White, TextAlignmentOptions.Center);
        Stretch(keyText.rectTransform, 0);
        TMP_Text labelText = AddText("Label", root.transform, label, 13, FontStyles.Bold, labelColor, TextAlignmentOptions.Center);
        SetRect(labelText.rectTransform, new Vector2(.5f,0), new Vector2(.5f,0), new Vector2(0,0), new Vector2(126,24), new Vector2(.5f,0));
    }

    private static GameObject RectGO(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, anchorMin, anchorMax, anchoredPos, sizeDelta, pivot);
        return go;
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(.5f,.5f);
        rt.offsetMin = new Vector2(inset,inset);
        rt.offsetMax = new Vector2(-inset,-inset);
    }

    private static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? type : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text AddText(string name, Transform parent, string textValue, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private static void ConfigureSprites()
    {
        string[] names = {
            "panel_dark.png", "panel_gold.png", "button.png", "keycap.png", "gauge_bg.png", "gauge_fill.png",
            "minimap_frame.png", "icon_compass.png", "icon_quest.png", "icon_attack.png", "icon_guard.png",
            "icon_dodge.png", "icon_jump.png", "icon_success.png"
        };
        foreach (string name in names)
        {
            string path = FindAssetPath(name);
            if (string.IsNullOrEmpty(path)) continue;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (name.StartsWith("panel_") || name == "button.png") importer.spriteBorder = new Vector4(18,18,18,18);
            else if (name == "keycap.png") importer.spriteBorder = new Vector4(14,14,14,14);
            else if (name.StartsWith("gauge_")) importer.spriteBorder = new Vector4(10,10,10,10);
            importer.SaveAndReimport();
        }
    }

    private static Sprite LoadSprite(string fileName)
    {
        string path = FindAssetPath(fileName);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static string FindAssetPath(string fileName)
    {
        string stem = System.IO.Path.GetFileNameWithoutExtension(fileName);
        string[] guids = AssetDatabase.FindAssets(stem);
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(System.IO.Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)) return p;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\','/');
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SavePrefab(GameObject source, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(source, path);
    }
}
#endif
