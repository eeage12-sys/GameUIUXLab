#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RPGPPMaterialFixAll
{
    [MenuItem("Tools/Hub/Fix All RPGPP Materials")]
    public static void FixAllRPGPPMaterials()
    {
        Material mainMat = FindMaterialExact("rpgpp_lt_mat_a");
        Texture2D mainTex = FindTextureExact("rpgpp_lt_tex_a");

        if (mainMat == null)
        {
            EditorUtility.DisplayDialog(
                "RPGPP 머티리얼 없음",
                "rpgpp_lt_mat_a 머티리얼을 찾지 못했습니다.\nRPGPP_LT 패키지가 정상적으로 Import 되었는지 확인해주세요.",
                "확인");
            return;
        }

        // 1) 메인 머티리얼 자체를 정상화
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
            mainMat.shader = urpLit;

        if (mainTex != null)
        {
            if (mainMat.HasProperty("_BaseMap"))
                mainMat.SetTexture("_BaseMap", mainTex);
            if (mainMat.HasProperty("_MainTex"))
                mainMat.SetTexture("_MainTex", mainTex);
        }

        if (mainMat.HasProperty("_BaseColor"))
            mainMat.SetColor("_BaseColor", Color.white);
        if (mainMat.HasProperty("_Color"))
            mainMat.SetColor("_Color", Color.white);

        EditorUtility.SetDirty(mainMat);

        // 2) 현재 열린 씬의 RPGPP 렌더러를 한 번에 교체
        Scene scene = SceneManager.GetActiveScene();
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int rendererCount = 0;
        int slotCount = 0;

        foreach (Renderer r in renderers)
        {
            if (r == null || r.gameObject == null || r.gameObject.scene != scene)
                continue;

            if (!IsRPGPPRenderer(r))
                continue;

            Material[] mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0)
                mats = new Material[1];

            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                // RPGPP 모델의 렌더러 슬롯만 메인 팔레트 머티리얼로 통일
                if (mats[i] != mainMat)
                {
                    mats[i] = mainMat;
                    slotCount++;
                    changed = true;
                }
            }

            if (changed)
            {
                Undo.RecordObject(r, "Fix RPGPP Materials");
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                rendererCount++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog(
            "RPGPP 일괄 수정 완료",
            $"RPGPP 오브젝트의 머티리얼을 한 번에 복구했습니다.\n\n수정된 Renderer: {rendererCount}\n교체된 Material 슬롯: {slotCount}\n\n이제 Ctrl+S로 씬을 저장해주세요.",
            "확인");
    }

    private static bool IsRPGPPRenderer(Renderer r)
    {
        // A. Prefab / FBX 원본 경로 확인
        UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(r.gameObject);
        if (source != null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source).Replace("\\", "/").ToLowerInvariant();
            if (sourcePath.Contains("rpgpp_lt") || sourcePath.Contains("/rpgpp/"))
                return true;
        }

        // B. Mesh 원본 경로 확인
        MeshFilter mf = r.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            string meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh).Replace("\\", "/").ToLowerInvariant();
            if (meshPath.Contains("rpgpp_lt") || meshPath.Contains("/rpgpp/"))
                return true;
        }

        SkinnedMeshRenderer smr = r as SkinnedMeshRenderer;
        if (smr != null && smr.sharedMesh != null)
        {
            string meshPath = AssetDatabase.GetAssetPath(smr.sharedMesh).Replace("\\", "/").ToLowerInvariant();
            if (meshPath.Contains("rpgpp_lt") || meshPath.Contains("/rpgpp/"))
                return true;
        }

        // C. 현재 머티리얼 이름으로 확인
        foreach (Material m in r.sharedMaterials)
        {
            if (m == null) continue;
            string n = m.name.ToLowerInvariant();
            if (n.StartsWith("rpgpp_lt_") && !n.Contains("cloud") && !n.Contains("sky"))
                return true;
        }

        // D. hierarchy 이름으로 마지막 보조 판단
        Transform t = r.transform;
        while (t != null)
        {
            string n = t.name.ToLowerInvariant();
            if (n.StartsWith("village_building_") ||
                n.StartsWith("market_") ||
                n.StartsWith("centerfiller_") ||
                n.StartsWith("village_shed_"))
                return true;
            t = t.parent;
        }

        return false;
    }

    private static Material FindMaterialExact(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        return null;
    }

    private static Texture2D FindTextureExact(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return null;
    }
}
#endif
