#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class HubCollisionSetupV2
{
    private const string AutoColliderName = "__AutoCollider_V2";

    [MenuItem("Tools/Hub/Rebuild Environment Colliders V2")]
    public static void RebuildEnvironmentColliders()
    {
        GameObject sceneRoot =
            GameObject.Find("Hub_Field_Lightweight_V2") ??
            GameObject.Find("Hub_Field_Lightweight");

        if (sceneRoot == null)
        {
            EditorUtility.DisplayDialog(
                "허브 루트 없음",
                "현재 씬에서 Hub_Field_Lightweight_V2 또는 Hub_Field_Lightweight를 찾지 못했습니다.",
                "확인"
            );
            return;
        }

        // 예전 자동 Collider까지 함께 제거
        RemoveAutoColliders(sceneRoot);

        int buildings = 0;
        int props = 0;
        int trees = 0;
        int rocks = 0;

        Transform village = sceneRoot.transform.Find("03_Village");
        Transform propRoot = sceneRoot.transform.Find("04_Props");
        Transform nature = sceneRoot.transform.Find("05_Nature_LowDensity");

        if (village != null)
        {
            foreach (Transform child in village)
            {
                string n = child.name.ToLowerInvariant();

                if (IsSmallDecoration(n))
                    continue;

                if (n.Contains("building"))
                {
                    // 지붕 돌출 때문에 벽보다 바깥에서 막히지 않도록 X/Z를 더 줄임
                    AddAccurateBoxCollider(child.gameObject, 0.72f, 0.92f);
                    buildings++;
                }
                else if (n.Contains("shed"))
                {
                    AddAccurateBoxCollider(child.gameObject, 0.78f, 0.90f);
                    buildings++;
                }
                else if (n.Contains("market"))
                {
                    AddAccurateBoxCollider(child.gameObject, 0.82f, 0.88f);
                    buildings++;
                }
            }
        }

        if (propRoot != null)
        {
            foreach (Transform child in propRoot)
            {
                string n = child.name.ToLowerInvariant();

                if (IsSmallDecoration(n))
                    continue;

                if (n.Contains("well") ||
                    n.Contains("wagon") ||
                    n.Contains("crate") ||
                    n.Contains("barrel") ||
                    n.Contains("fence") ||
                    n.Contains("bench") ||
                    n.Contains("table"))
                {
                    AddAccurateBoxCollider(child.gameObject, 0.84f, 0.90f);
                    props++;
                }
            }
        }

        if (nature != null)
        {
            foreach (Transform child in nature)
            {
                string n = child.name.ToLowerInvariant();

                if (IsSmallDecoration(n))
                    continue;

                if (n.Contains("tree"))
                {
                    AddTreeTrunkCollider(child.gameObject);
                    trees++;
                }
                else if (n.Contains("rock") || n.Contains("menhir") || n.Contains("rune"))
                {
                    AddAccurateBoxCollider(child.gameObject, 0.78f, 0.90f);
                    rocks++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Collider V2 완료",
            $"건물/시장: {buildings}\n" +
            $"큰 소품: {props}\n" +
            $"나무: {trees}\n" +
            $"바위/룬석: {rocks}\n\n" +
            "이전보다 보이는 형태에 가깝게 Collider를 다시 만들었습니다.",
            "확인"
        );
    }

    [MenuItem("Tools/Hub/Remove Environment Colliders V2")]
    public static void RemoveEnvironmentColliders()
    {
        GameObject sceneRoot =
            GameObject.Find("Hub_Field_Lightweight_V2") ??
            GameObject.Find("Hub_Field_Lightweight");

        if (sceneRoot == null)
            return;

        int removed = RemoveAutoColliders(sceneRoot);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Collider 제거 완료",
            $"{removed}개의 자동 Collider 오브젝트를 제거했습니다.",
            "확인"
        );
    }

    private static int RemoveAutoColliders(GameObject sceneRoot)
    {
        List<GameObject> deleteList = new List<GameObject>();

        Transform[] all = sceneRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name == "__AutoCollider" || t.name == AutoColliderName)
                deleteList.Add(t.gameObject);
        }

        foreach (GameObject go in deleteList)
            Undo.DestroyObjectImmediate(go);

        return deleteList.Count;
    }

    private static bool IsSmallDecoration(string n)
    {
        return n.Contains("grass") ||
               n.Contains("flower") ||
               n.Contains("poppy") ||
               n.Contains("mushroom") ||
               n.Contains("shrub") ||
               n.Contains("bush") ||
               n.Contains("plant") ||
               n.Contains("weed");
    }

    private static void AddAccurateBoxCollider(
        GameObject root,
        float horizontalScale,
        float verticalScale)
    {
        if (!TryGetLocalMeshBounds(root.transform, out Bounds localBounds))
            return;

        GameObject holder = new GameObject(AutoColliderName);
        Undo.RegisterCreatedObjectUndo(holder, "Create Environment Collider");

        holder.transform.SetParent(root.transform, false);
        holder.transform.localPosition = localBounds.center;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;

        BoxCollider box = Undo.AddComponent<BoxCollider>(holder);
        box.center = Vector3.zero;
        box.size = new Vector3(
            Mathf.Max(0.20f, localBounds.size.x * horizontalScale),
            Mathf.Max(0.20f, localBounds.size.y * verticalScale),
            Mathf.Max(0.20f, localBounds.size.z * horizontalScale)
        );
    }

    private static bool TryGetLocalMeshBounds(Transform root, out Bounds result)
    {
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        bool initialized = false;
        result = new Bounds();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            Bounds meshBounds = mf.sharedMesh.bounds;
            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 meshLocalPoint = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );

                        Vector3 worldPoint = mf.transform.TransformPoint(meshLocalPoint);
                        Vector3 rootLocalPoint = root.InverseTransformPoint(worldPoint);

                        if (!initialized)
                        {
                            result = new Bounds(rootLocalPoint, Vector3.zero);
                            initialized = true;
                        }
                        else
                        {
                            result.Encapsulate(rootLocalPoint);
                        }
                    }
                }
            }
        }

        return initialized;
    }

    private static void AddTreeTrunkCollider(GameObject root)
    {
        if (!TryGetLocalMeshBounds(root.transform, out Bounds b))
            return;

        float height = Mathf.Max(1f, b.size.y);
        float trunkHeight = Mathf.Clamp(height * 0.58f, 1.2f, 5.0f);

        float width = Mathf.Min(b.size.x, b.size.z);
        float trunkRadius = Mathf.Clamp(width * 0.10f, 0.18f, 0.48f);

        GameObject holder = new GameObject(AutoColliderName);
        Undo.RegisterCreatedObjectUndo(holder, "Create Tree Collider");

        holder.transform.SetParent(root.transform, false);
        holder.transform.localPosition =
            new Vector3(b.center.x, b.min.y + trunkHeight * 0.5f, b.center.z);
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;

        CapsuleCollider capsule = Undo.AddComponent<CapsuleCollider>(holder);
        capsule.direction = 1;
        capsule.center = Vector3.zero;
        capsule.height = trunkHeight;
        capsule.radius = trunkRadius;
    }
}
#endif
