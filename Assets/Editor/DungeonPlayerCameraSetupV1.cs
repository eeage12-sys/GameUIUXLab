using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Copies the EXACT Player and third-person camera currently used in the Hub
/// into NGF_CompactDungeon, preserving the current component/settings setup.
/// It also links the copied PlayerMovement to the copied camera and disables
/// the old dungeon preview camera.
/// </summary>
public static class DungeonPlayerCameraSetupV1
{
    private const string HubSceneName = "Hub_Field_Lightweight_V2";
    private const string DungeonScenePath = "Assets/Scenes/Dungeon/NGF_CompactDungeon.unity";

    [MenuItem("Tools/Game/Dungeon/1. Copy Hub Player + Camera To Dungeon")]
    public static void SetupDungeonPlayerAndCamera()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Dungeon Setup", "Play 모드를 먼저 종료해 주세요.", "OK");
            return;
        }

        Scene hubScene = SceneManager.GetActiveScene();

        PlayerMovement hubMovement = FindComponentInScene<PlayerMovement>(hubScene);
        if (hubMovement == null)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Setup",
                "현재 열린 씬에서 PlayerMovement가 붙은 Player를 찾지 못했습니다.\n\nHub_Field_Lightweight_V2 씬을 연 뒤 다시 실행해 주세요.",
                "OK");
            return;
        }

        GameObject hubPlayer = hubMovement.gameObject;

        ThirdPersonCamera hubThirdPersonCamera = FindComponentInScene<ThirdPersonCamera>(hubScene);
        if (hubThirdPersonCamera == null)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Setup",
                "현재 열린 씬에서 ThirdPersonCamera가 붙은 카메라를 찾지 못했습니다.\n\nHubPreviewCamera가 있는 허브 씬에서 다시 실행해 주세요.",
                "OK");
            return;
        }

        GameObject hubCamera = hubThirdPersonCamera.gameObject;

        if (!System.IO.File.Exists(DungeonScenePath))
        {
            EditorUtility.DisplayDialog(
                "Dungeon Setup",
                "던전 씬을 찾지 못했습니다.\n\n" + DungeonScenePath,
                "OK");
            return;
        }

        // Save the current Hub before cloning its exact current setup.
        if (hubScene.isDirty)
            EditorSceneManager.SaveScene(hubScene);

        Scene dungeonScene = EditorSceneManager.OpenScene(DungeonScenePath, OpenSceneMode.Additive);

        try
        {
            Transform playerSpawn = FindTransformInScene(dungeonScene, "PlayerSpawn");
            if (playerSpawn == null)
            {
                EditorUtility.DisplayDialog(
                    "Dungeon Setup",
                    "던전 씬에서 PlayerSpawn 마커를 찾지 못했습니다.\n\n기존 NGF_CompactDungeon 생성 씬인지 확인해 주세요.",
                    "OK");
                return;
            }

            // The generator created a marker parent named "Player".
            // Rename it so it cannot be confused with the actual playable Player.
            Transform markerParent = playerSpawn.parent;
            if (markerParent != null &&
                markerParent.name == "Player" &&
                markerParent.GetComponent<PlayerMovement>() == null)
            {
                markerParent.name = "PlayerMarkers";
            }

            // Remove only previously generated playable copies from the dungeon.
            RemoveExistingPlayablePlayer(dungeonScene);
            RemoveExistingDungeonPlayerCamera(dungeonScene);

            // Clone the EXACT hub player instance (including current overrides/settings).
            GameObject dungeonPlayer = Object.Instantiate(hubPlayer);
            dungeonPlayer.name = "Player";
            SceneManager.MoveGameObjectToScene(dungeonPlayer, dungeonScene);
            dungeonPlayer.transform.SetParent(null);
            dungeonPlayer.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            dungeonPlayer.SetActive(true);

            // Clone the EXACT hub third-person camera instance.
            GameObject dungeonCamera = Object.Instantiate(hubCamera);
            dungeonCamera.name = "DungeonPlayerCamera";
            SceneManager.MoveGameObjectToScene(dungeonCamera, dungeonScene);
            dungeonCamera.transform.SetParent(null);
            dungeonCamera.SetActive(true);

            // Re-link references that originally pointed back to Hub objects.
            PlayerMovement dungeonMovement = dungeonPlayer.GetComponent<PlayerMovement>();
            if (dungeonMovement == null)
                dungeonMovement = dungeonPlayer.GetComponentInChildren<PlayerMovement>(true);

            ThirdPersonCamera dungeonThirdPersonCamera = dungeonCamera.GetComponent<ThirdPersonCamera>();
            if (dungeonThirdPersonCamera == null)
                dungeonThirdPersonCamera = dungeonCamera.GetComponentInChildren<ThirdPersonCamera>(true);

            if (dungeonMovement != null)
                dungeonMovement.cameraTransform = dungeonCamera.transform;

            if (dungeonThirdPersonCamera != null)
                dungeonThirdPersonCamera.target = dungeonPlayer.transform;

            // The dungeon generator uses a top-down MapPreviewCamera.
            // Disable every other dungeon camera so only the copied third-person camera renders.
            foreach (Camera cam in FindComponentsInScene<Camera>(dungeonScene))
            {
                if (cam == null || cam.gameObject == dungeonCamera)
                    continue;

                cam.enabled = false;

                AudioListener oldListener = cam.GetComponent<AudioListener>();
                if (oldListener != null)
                    oldListener.enabled = false;

                if (cam.gameObject.CompareTag("MainCamera"))
                    cam.gameObject.tag = "Untagged";
            }

            Camera playerCamera = dungeonCamera.GetComponent<Camera>();
            if (playerCamera != null)
                playerCamera.enabled = true;

            AudioListener playerListener = dungeonCamera.GetComponent<AudioListener>();
            if (playerListener != null)
                playerListener.enabled = true;

            dungeonCamera.tag = "MainCamera";

            EditorSceneManager.MarkSceneDirty(dungeonScene);
            EditorSceneManager.SaveScene(dungeonScene);

            Debug.Log(
                "[Dungeon Setup] 완료: Hub의 Player + ThirdPersonCamera를 NGF_CompactDungeon에 복사하고 PlayerSpawn에 배치했습니다.");

            EditorUtility.DisplayDialog(
                "Dungeon Setup 완료",
                "던전에 현재 허브의 Player와 3인칭 카메라를 그대로 복사했습니다.\n\n" +
                "• Player → PlayerSpawn 배치\n" +
                "• Hub 카메라 설정 그대로 복사\n" +
                "• PlayerMovement Camera Transform 자동 연결\n" +
                "• ThirdPersonCamera Target 자동 연결\n" +
                "• 기존 MapPreviewCamera 비활성화\n\n" +
                "이제 허브에서 Play → 포탈 → E로 다시 테스트하세요.",
                "OK");
        }
        finally
        {
            // Return to the Hub after editing the dungeon.
            if (dungeonScene.IsValid() && dungeonScene.isLoaded)
                EditorSceneManager.CloseScene(dungeonScene, true);

            if (hubScene.IsValid() && hubScene.isLoaded)
                SceneManager.SetActiveScene(hubScene);
        }
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
    {
        System.Collections.Generic.List<T> results = new System.Collections.Generic.List<T>();

        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));

        return results.ToArray();
    }

    private static Transform FindTransformInScene(Scene scene, string exactName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == exactName)
                    return t;
            }
        }

        return null;
    }

    private static void RemoveExistingPlayablePlayer(Scene dungeonScene)
    {
        foreach (GameObject root in dungeonScene.GetRootGameObjects())
        {
            PlayerMovement movement = root.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                Object.DestroyImmediate(root);
                return;
            }
        }
    }

    private static void RemoveExistingDungeonPlayerCamera(Scene dungeonScene)
    {
        foreach (GameObject root in dungeonScene.GetRootGameObjects())
        {
            if (root.name == "DungeonPlayerCamera")
            {
                Object.DestroyImmediate(root);
                return;
            }
        }
    }
}
