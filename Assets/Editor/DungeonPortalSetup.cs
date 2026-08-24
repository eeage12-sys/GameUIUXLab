#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DungeonPortalSetup
{
    [MenuItem("Tools/Game/Portal/1. Setup Hub Dungeon Portal")]
    public static void SetupHubPortal()
    {
        GameObject triggerObject = GameObject.Find("DungeonPortalTrigger");
        if (triggerObject == null)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Portal Setup",
                "Could not find a GameObject named 'DungeonPortalTrigger' in the open scene.",
                "OK");
            return;
        }

        BoxCollider box = triggerObject.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(triggerObject);

        Undo.RecordObject(box, "Configure Dungeon Portal Trigger");
        box.isTrigger = true;
        EditorUtility.SetDirty(box);

        DungeonPortal portal = triggerObject.GetComponent<DungeonPortal>();
        if (portal == null)
            portal = Undo.AddComponent<DungeonPortal>(triggerObject);

        Selection.activeGameObject = triggerObject;
        EditorGUIUtility.PingObject(triggerObject);
        EditorUtility.SetDirty(triggerObject);

        EditorUtility.DisplayDialog(
            "Dungeon Portal Setup",
            "DungeonPortalTrigger is ready.\n\nPlay the Hub scene, walk into the portal, and press E.",
            "OK");
    }
}
#endif
