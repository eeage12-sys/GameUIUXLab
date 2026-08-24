#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class OriginUIPortfolioValidator
{
    [MenuItem("Tools/Game UI/ORIGIN UI/7. Validate Portfolio Requirements")]
    public static void ValidatePortfolioRequirements()
    {
        List<string> passed = new List<string>();
        List<string> failed = new List<string>();
        GameObject root = GameObject.Find("ORIGIN_UI");

        Check(root != null, "ORIGIN_UI root exists", passed, failed);
        if (root != null)
        {
            Canvas canvas = root.GetComponent<Canvas>();
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            Check(canvas != null, "UGUI Canvas exists", passed, failed);
            Check(scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                "Canvas Scaler = Scale With Screen Size", passed, failed);
            Check(scaler != null && scaler.referenceResolution == new Vector2(1920, 1080),
                "Reference Resolution = 1920x1080", passed, failed);
            Check(root.GetComponentsInChildren<TMP_Text>(true).Length > 0,
                "TextMeshPro text is used", passed, failed);

            Transform hud = root.transform.Find("HUD");
            Transform pause = root.transform.Find("PauseMenu");
            Transform result = root.transform.Find("ResultMenu");
            Transform title = root.transform.Find("TitleScreen");
            Check(hud != null, "Play HUD exists", passed, failed);
            Check(pause != null, "Pause menu exists", passed, failed);
            Check(result != null, "Result menu exists", passed, failed);
            Check(title != null, "Title screen exists", passed, failed);

            OriginUIState state = root.GetComponent<OriginUIState>();
            OriginHUDController hudController = root.GetComponent<OriginHUDController>();
            OriginUIFlowController flow = root.GetComponent<OriginUIFlowController>();
            OriginUISceneContext sceneContext = root.GetComponent<OriginUISceneContext>();
            Check(state != null && hudController != null, "HUD is connected to script data", passed, failed);
            Check(flow != null, "Menu flow controller exists", passed, failed);
            Check(sceneContext != null, "Hub/Dungeon scene UI rule exists", passed, failed);

            int buttons = root.GetComponentsInChildren<Button>(true).Length;
            Check(buttons >= 2, "Clickable UI buttons exist", passed, failed);
            Check(root.GetComponentsInChildren<OriginUIButtonFeedback>(true).Length >= 1,
                "Button state feedback exists", passed, failed);
            Check(root.GetComponent<OriginToastController>() != null,
                "Toast notification feedback exists", passed, failed);
        }

        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        Check(eventSystem != null, "EventSystem exists", passed, failed);
#if ENABLE_INPUT_SYSTEM
        Check(eventSystem != null && eventSystem.GetComponent<InputSystemUIInputModule>() != null,
            "Input System UI module exists", passed, failed);
#endif

        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/MenuButton.prefab") != null,
            "Reusable prefab: MenuButton", passed, failed);
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/GaugeBar.prefab") != null,
            "Reusable prefab: GaugeBar", passed, failed);
        Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ToastMessage.prefab") != null,
            "Reusable prefab: ToastMessage", passed, failed);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"AUTO CHECK: {passed.Count} passed / {failed.Count} failed");
        sb.AppendLine();
        foreach (string item in passed) sb.AppendLine("PASS  " + item);
        foreach (string item in failed) sb.AppendLine("FAIL  " + item);
        sb.AppendLine();
        sb.AppendLine("MANUAL CHECK STILL REQUIRED:");
        sb.AppendLine("- Mouse click test");
        sb.AppendLine("- Keyboard menu move + Enter Submit + Esc Cancel");
        sb.AppendLine("- Resolution change test");
        sb.AppendLine("- Record actual result / fix / re-test for at least 2 UI items");
        sb.AppendLine("- Capture at least 2 gameplay screenshots for submission");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("ORIGIN UI Portfolio Validator",
            failed.Count == 0
                ? "Automatic structure check passed.\n\nManual tests are still required for the assignment.\nSee Console and Docs/PORTFOLIO_REQUIREMENTS.md."
                : $"{failed.Count} automatic checks failed.\nSee Console for details.",
            "OK");
    }

    private static void Check(bool condition, string label, List<string> passed, List<string> failed)
    {
        if (condition) passed.Add(label); else failed.Add(label);
    }
}
#endif
