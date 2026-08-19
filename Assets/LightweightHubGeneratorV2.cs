#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightweightHubGeneratorV2
{
    private const string ScenePath = "Assets/Scenes/Hub/Hub_Field_Lightweight_V2.unity";
    private const string GeneratedFolder = "Assets/GeneratedHubLightweightV2";
    private const string TerrainDataPath = GeneratedFolder + "/HubTerrain_Lightweight_V2.asset";
    private const string GrassLayerPath = GeneratedFolder + "/HubGrass_Lightweight_V2.terrainlayer";
    private const string DirtLayerPath = GeneratedFolder + "/HubDirt_Lightweight_V2.terrainlayer";
    private const string WaterMaterialPath = GeneratedFolder + "/HubWater_Lightweight_V2.mat";
    private const string PortalMaterialPath = GeneratedFolder + "/HubPortal_Lightweight_V2.mat";

    private const float TerrainSize = 150f;
    private const float TerrainHeight = 18f;
    private static readonly Vector3 TerrainOrigin = new Vector3(-75f, 0f, -75f);

    private static readonly Vector3 SpawnPoint = new Vector3(0f, 0f, -58f);
    private static readonly Vector3 VillageCenter = new Vector3(0f, 0f, -4f);
    private static readonly Vector3 BridgeCenter = new Vector3(0f, 0f, 31f);
    private static readonly Vector3 PortalCenter = new Vector3(0f, 0f, 55f);

    private static System.Random rng;
    private static Material rpgppMaterial;

    [MenuItem("Tools/Hub/Generate Lightweight Hub V2 (Clean)")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        rng = new System.Random(20260819);

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Scenes/Hub");
        EnsureFolder(GeneratedFolder);

        // RPGPP_LT - 마을에만 사용
        GameObject[] buildings =
        {
            FindPrefab("rpgpp_lt_building_01", "RPGPP_LT"),
            FindPrefab("rpgpp_lt_building_02", "RPGPP_LT"),
            FindPrefab("rpgpp_lt_building_03", "RPGPP_LT"),
            FindPrefab("rpgpp_lt_building_04", "RPGPP_LT"),
            FindPrefab("rpgpp_lt_building_05", "RPGPP_LT")
        };

        if (buildings.All(x => x == null))
        {
            EditorUtility.DisplayDialog("RPGPP_LT를 찾지 못했습니다", "RPGPP_LT가 프로젝트에 Import 되어 있는지 확인해주세요.", "확인");
            return;
        }

        GameObject well = FindPrefab("rpgpp_lt_well_01", "RPGPP_LT");
        GameObject awningA = FindPrefab("rpgpp_lt_awning_standing_01a", "RPGPP_LT");
        GameObject awningB = FindPrefab("rpgpp_lt_awning_standing_01b", "RPGPP_LT");
        GameObject benchA = FindPrefab("rpgpp_lt_bench_wood_01", "RPGPP_LT");
        GameObject benchB = FindPrefab("rpgpp_lt_bench_wood_02", "RPGPP_LT");
        GameObject table = FindPrefab("rpgpp_lt_table_01", "RPGPP_LT");
        GameObject barrel = FindPrefab("rpgpp_lt_barrel_01", "RPGPP_LT");
        GameObject crate = FindPrefab("rpgpp_lt_crate_01", "RPGPP_LT");
        GameObject sack = FindPrefab("rpgpp_lt_sack_01", "RPGPP_LT");
        GameObject wagon = FindPrefab("rpgpp_lt_wagon_01", "RPGPP_LT");
        GameObject shed = FindPrefab("rpgpp_lt_shed_wood_01", "RPGPP_LT");
        GameObject fence = FindPrefab("rpgpp_lt_fence_wood_01a", "RPGPP_LT");
        GameObject bannerA = FindPrefab("rpgpp_lt_banner_01a", "RPGPP_LT");
        GameObject bannerB = FindPrefab("rpgpp_lt_banner_01b", "RPGPP_LT");

        // Polytope - 자연물에만 사용
        GameObject genericTree = FindPrefab("PT_Generic_Tree_01_green", "Lowpoly_Environments");
        GameObject pineTree = FindPrefab("PT_Pine_Tree_03_green", "Lowpoly_Environments");
        GameObject fruitTree = FindPrefab("PT_Fruit_Tree_01_green", "Lowpoly_Environments");
        GameObject shrub = FindPrefab("PT_Generic_Shrub_01_green", "Lowpoly_Environments");
        GameObject grassA = FindPrefab("PT_Grass_01", "Lowpoly_Environments");
        GameObject grassB = FindPrefab("PT_Grass_02", "Lowpoly_Environments");
        GameObject poppy = FindPrefab("PT_Poppy_02", "Lowpoly_Environments");
        GameObject rock = FindPrefab("PT_Generic_Rock_01", "Lowpoly_Environments");
        GameObject riverRock = FindPrefab("PT_River_Rock_Pile_02", "Lowpoly_Environments");
        GameObject menhir = FindPrefab("PT_Menhir_Rock_02", "Lowpoly_Environments");
        GameObject woodenBridge = FindPrefab("PT_Wooden_Bridge_02", "Lowpoly_Village");

        Texture2D grassTexture = FindTexture("PT_Ground_Grass_Green_01", "Lowpoly_Environments");
        Texture2D dirtTexture = FindTexture("PT_Ground_Generic_03", "Lowpoly_Environments");
        Material skybox = FindMaterial("PT_Skybox_mat", "Polytope Studio");
        Material sourceWater = FindMaterial("PT_Water_mat", "Polytope Studio");

        rpgppMaterial = PrepareRpgppMaterial();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("Hub_Field_Lightweight_V2");
        Transform terrainRoot = MakeGroup(root.transform, "01_Terrain");
        Transform pathRoot = MakeGroup(root.transform, "02_Path");
        Transform villageRoot = MakeGroup(root.transform, "03_Village");
        Transform propsRoot = MakeGroup(root.transform, "04_Props");
        Transform natureRoot = MakeGroup(root.transform, "05_Nature_LowDensity");
        Transform waterRoot = MakeGroup(root.transform, "06_Creek");
        Transform portalRoot = MakeGroup(root.transform, "07_Portal");
        Transform lightingRoot = MakeGroup(root.transform, "08_Lighting");
        Transform markerRoot = MakeGroup(root.transform, "09_GameplayMarkers");

        Terrain terrain = CreateTerrain(terrainRoot, grassTexture, dirtTexture);
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Terrain 생성 실패", "Terrain을 생성하지 못했습니다.", "확인");
            return;
        }

        BuildVillage(terrain, villageRoot, propsRoot, buildings, well, awningA, awningB, benchA, benchB, table, barrel, crate, sack, wagon, shed, fence, bannerA, bannerB);
        BuildNatureLowDensity(terrain, natureRoot, genericTree, pineTree, fruitTree, shrub, grassA, grassB, poppy, rock, riverRock, menhir);
        BuildCreekAndBridge(terrain, waterRoot, propsRoot, sourceWater, woodenBridge, riverRock, grassA, poppy);
        BuildPortal(terrain, portalRoot, menhir, rock);
        BuildLighting(lightingRoot, skybox);
        BuildMarkers(terrain, markerRoot);
        BuildPreviewCamera(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        EditorUtility.DisplayDialog(
            "경량 허브 생성 완료",
            "새 씬을 만들었습니다. 기존 허브는 건드리지 않았습니다.\n\n" +
            "맵 크기: 기존보다 크게 축소\n" +
            "마을: RPGPP_LT 구상 유지\n" +
            "자연물: Polytope만 사용, 수량 대폭 감소\n" +
            "조명: Directional Light 1개만 사용\n" +
            "나무 다리 / 개울 / 룬석 / 포인트 컬러 나무 포함\n\n" +
            ScenePath,
            "확인");
    }

    private static Terrain CreateTerrain(Transform parent, Texture2D grassTexture, Texture2D dirtTexture)
    {
        DeleteIfExists(TerrainDataPath);
        DeleteIfExists(GrassLayerPath);
        DeleteIfExists(DirtLayerPath);

        TerrainData data = new TerrainData();
        data.heightmapResolution = 257;
        data.alphamapResolution = 256;
        data.baseMapResolution = 512;
        data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float nz = z / (float)(resolution - 1);
                float wx = nx * TerrainSize - TerrainSize * 0.5f;
                float wz = nz * TerrainSize - TerrainSize * 0.5f;

                float n1 = Mathf.PerlinNoise(nx * 2.8f + 2.1f, nz * 2.8f + 7.4f);
                float n2 = Mathf.PerlinNoise(nx * 6.0f + 1.3f, nz * 6.0f + 4.6f);
                float h = 1.0f + (n1 - 0.5f) * 2.0f + (n2 - 0.5f) * 0.55f;

                // 외곽에만 완만한 언덕. 플레이 구간은 비교적 평탄하게 유지.
                float edge = Mathf.Clamp01((Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz)) - 48f) / 25f);
                h += edge * edge * 3.2f;

                float villageDistance = Vector2.Distance(new Vector2(wx, wz), new Vector2(VillageCenter.x, VillageCenter.z));
                float villageFlat = 1f - Smooth01(18f, 29f, villageDistance);
                h = Mathf.Lerp(h, 1.0f, villageFlat);

                float creekDistance = Mathf.Abs(wz - CreekZAt(wx));
                float creekCut = 1f - Smooth01(3.0f, 7.0f, creekDistance);
                h -= creekCut * 1.1f;

                float pathDistance = DistanceToMainPath(wx, wz);
                float pathFlat = 1f - Smooth01(2.5f, 6.0f, pathDistance);
                h = Mathf.Lerp(h, 1.0f, pathFlat * 0.70f);

                heights[z, x] = Mathf.Clamp01(h / TerrainHeight);
            }
        }

        data.SetHeights(0, 0, heights);
        AssetDatabase.CreateAsset(data, TerrainDataPath);

        List<TerrainLayer> layers = new List<TerrainLayer>();
        if (grassTexture != null)
        {
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = grassTexture;
            layer.tileSize = new Vector2(14f, 14f);
            AssetDatabase.CreateAsset(layer, GrassLayerPath);
            layers.Add(layer);
        }

        if (dirtTexture != null)
        {
            TerrainLayer layer = new TerrainLayer();
            layer.diffuseTexture = dirtTexture;
            layer.tileSize = new Vector2(7f, 7f);
            AssetDatabase.CreateAsset(layer, DirtLayerPath);
            layers.Add(layer);
        }

        if (layers.Count > 0)
        {
            data.terrainLayers = layers.ToArray();
            PaintTerrain(data, layers.Count >= 2);
        }

        GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "LightweightTerrain";
        terrainObject.transform.SetParent(parent, false);
        terrainObject.transform.position = TerrainOrigin;

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 8f;
        terrain.basemapDistance = 300f;
        terrain.drawInstanced = true;
        terrain.treeDistance = 130f;
        terrain.detailObjectDistance = 45f;
        return terrain;
    }

    private static void PaintTerrain(TerrainData data, bool hasDirt)
    {
        int resolution = data.alphamapResolution;
        int layerCount = hasDirt ? 2 : 1;
        float[,,] map = new float[resolution, resolution, layerCount];

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = x / (float)(resolution - 1);
                float nz = z / (float)(resolution - 1);
                float wx = nx * TerrainSize - TerrainSize * 0.5f;
                float wz = nz * TerrainSize - TerrainSize * 0.5f;

                float dirt = 0f;
                if (hasDirt)
                {
                    float pathDistance = DistanceToMainPath(wx, wz);
                    float path = 1f - Smooth01(2.1f, 4.6f, pathDistance);
                    float plaza = 1f - Smooth01(9f, 18f, Vector2.Distance(new Vector2(wx, wz), new Vector2(VillageCenter.x, VillageCenter.z)));
                    dirt = Mathf.Clamp01(Mathf.Max(path, plaza * 0.28f));
                }

                map[z, x, 0] = 1f - dirt;
                if (hasDirt)
                    map[z, x, 1] = dirt;
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    private static void BuildVillage(Terrain terrain, Transform villageRoot, Transform propsRoot,
        GameObject[] buildings, GameObject well, GameObject awningA, GameObject awningB,
        GameObject benchA, GameObject benchB, GameObject table, GameObject barrel, GameObject crate,
        GameObject sack, GameObject wagon, GameObject shed, GameObject fence, GameObject bannerA, GameObject bannerB)
    {
        Vector3[] positions =
        {
            new Vector3(-20f, 0f, -5f),
            new Vector3(19f, 0f, -5f),
            new Vector3(-15f, 0f, 14f),
            new Vector3(15f, 0f, 14f),
            new Vector3(0f, 0f, 21f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject prefab = buildings[i];
            if (prefab == null) continue;
            Quaternion rotation = FaceToward(positions[i], VillageCenter + new Vector3(0f, 0f, 6f));
            GameObject go = PlaceAtGround(terrain, prefab, positions[i], rotation, villageRoot, "Village_Building_" + (i + 1));
            ApplyRpgppMaterial(go);
        }

        GameObject placed;

        placed = PlaceAtGround(terrain, well, new Vector3(0f, 0f, 5f), Quaternion.identity, propsRoot, "Village_Well");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, awningA, new Vector3(-6f, 0f, 7f), Quaternion.Euler(0f, 18f, 0f), villageRoot, "Market_A");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, awningB, new Vector3(6f, 0f, 7f), Quaternion.Euler(0f, -18f, 0f), villageRoot, "Market_B");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, table, new Vector3(-6f, 0f, 6.8f), Quaternion.Euler(0f, 18f, 0f), propsRoot, "Market_Table_A");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, table, new Vector3(6f, 0f, 6.8f), Quaternion.Euler(0f, -18f, 0f), propsRoot, "Market_Table_B");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, benchA, new Vector3(-4f, 0f, 2f), Quaternion.Euler(0f, 90f, 0f), propsRoot, "Village_Bench_A");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, benchB, new Vector3(4f, 0f, 2f), Quaternion.Euler(0f, -90f, 0f), propsRoot, "Village_Bench_B");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, barrel, new Vector3(-8f, 0f, 9f), Quaternion.identity, propsRoot, "Village_Barrel");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, crate, new Vector3(-7f, 0f, 9f), Quaternion.Euler(0f, 25f, 0f), propsRoot, "Village_Crate");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, sack, new Vector3(8f, 0f, 9f), Quaternion.identity, propsRoot, "Village_Sack");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, wagon, new Vector3(-24f, 0f, 10f), Quaternion.Euler(0f, 72f, 0f), propsRoot, "Village_Wagon");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, shed, new Vector3(24f, 0f, 12f), Quaternion.Euler(0f, -90f, 0f), villageRoot, "Village_Shed");
        ApplyRpgppMaterial(placed);

        placed = PlaceAtGround(terrain, bannerA, new Vector3(-3f, 0f, -17f), Quaternion.identity, propsRoot, "Village_Banner_A");
        ApplyRpgppMaterial(placed);
        placed = PlaceAtGround(terrain, bannerB, new Vector3(3f, 0f, -17f), Quaternion.identity, propsRoot, "Village_Banner_B");
        ApplyRpgppMaterial(placed);

        // 짧은 울타리만 사용. 이전처럼 마을을 빽빽하게 둘러싸지 않음.
        for (int i = 0; i < 4; i++)
        {
            placed = PlaceAtGround(terrain, fence, new Vector3(25f + i * 2.1f, 0f, 20f), Quaternion.identity, propsRoot, "Garden_Fence_" + i);
            ApplyRpgppMaterial(placed);
        }
    }

    private static void BuildNatureLowDensity(Terrain terrain, Transform parent,
        GameObject genericTree, GameObject pineTree, GameObject fruitTree, GameObject shrub,
        GameObject grassA, GameObject grassB, GameObject poppy,
        GameObject rock, GameObject riverRock, GameObject menhir)
    {
        // 큰 나무 총 34그루 정도. 중앙 동선은 비워두고 외곽에만 군집.
        Scatter(terrain, parent, new[] { genericTree, pineTree }, 18, -69f, 69f, -70f, 70f, IsOuterTreeSpot, 0.95f, 1.15f, "TreeOuter");
        Scatter(terrain, parent, new[] { genericTree, fruitTree }, 10, -58f, 58f, -48f, 48f, IsMidTreeSpot, 0.95f, 1.10f, "TreeMid");
        Scatter(terrain, parent, new[] { pineTree }, 6, -68f, 68f, -68f, 68f, IsOuterTreeSpot, 1.00f, 1.18f, "PineAccent");

        // 작은 식물은 정말 소량만.
        Scatter(terrain, parent, new[] { shrub }, 16, -63f, 63f, -61f, 61f, IsSmallNatureSpot, 0.85f, 1.05f, "Shrub");
        Scatter(terrain, parent, new[] { grassA, grassB }, 30, -62f, 62f, -61f, 61f, IsGrassSpot, 0.90f, 1.10f, "Grass");
        Scatter(terrain, parent, new[] { poppy }, 14, -60f, 60f, -58f, 58f, IsGrassSpot, 0.90f, 1.05f, "Flower");
        Scatter(terrain, parent, new[] { rock, riverRock }, 10, -64f, 64f, -62f, 62f, IsRockSpot, 0.80f, 1.15f, "Rock");

        // 룬석은 포인트 3개만.
        PlaceAtGround(terrain, menhir, new Vector3(-28f, 0f, -37f), Quaternion.Euler(0f, 15f, 0f), parent, "RuneStone_Start", 0.95f);
        PlaceAtGround(terrain, menhir, new Vector3(32f, 0f, 20f), Quaternion.Euler(0f, -18f, 0f), parent, "RuneStone_Village", 1.00f);
        PlaceAtGround(terrain, menhir, new Vector3(-23f, 0f, 48f), Quaternion.Euler(0f, 22f, 0f), parent, "RuneStone_PortalRoad", 0.92f);

        // 색 포인트 나무 3그루만 남김.
        PlaceTintedTree(terrain, fruitTree != null ? fruitTree : genericTree, new Vector3(-22f, 0f, -29f), parent, "PinkTree", new Color(1.00f, 0.72f, 0.88f), 1.08f);
        PlaceTintedTree(terrain, fruitTree != null ? fruitTree : genericTree, new Vector3(29f, 0f, 19f), parent, "OrangeTree", new Color(1.00f, 0.52f, 0.14f), 1.05f);
        PlaceTintedTree(terrain, fruitTree != null ? fruitTree : genericTree, new Vector3(-29f, 0f, 43f), parent, "RedTree", new Color(0.92f, 0.20f, 0.14f), 1.08f);

        // 시작 지점은 시야를 가리지 않는 정도의 연출만.
        PlaceAtGround(terrain, genericTree, new Vector3(-17f, 0f, -52f), Quaternion.Euler(0f, 30f, 0f), parent, "StartTree_Left", 1.02f);
        PlaceAtGround(terrain, pineTree, new Vector3(18f, 0f, -50f), Quaternion.Euler(0f, -25f, 0f), parent, "StartTree_Right", 1.03f);
        PlaceAtGround(terrain, grassA, new Vector3(-5f, 0f, -54f), Quaternion.identity, parent, "StartGrass_A", 1.05f);
        PlaceAtGround(terrain, poppy, new Vector3(6f, 0f, -51f), Quaternion.identity, parent, "StartFlower_A", 1.00f);

        // 작은 자연물은 장식용이므로 Collider 비활성화.
        DisableCollidersByName(parent, new[] { "Grass", "Flower", "Shrub" });
    }

    private static void BuildCreekAndBridge(Terrain terrain, Transform creekRoot, Transform propsRoot,
        Material sourceWater, GameObject bridge, GameObject riverRock, GameObject grass, GameObject poppy)
    {
        Material water = CreateWaterMaterial(sourceWater);

        // 개울은 단 5개 세그먼트만 사용.
        float[] xs = { -70f, -42f, -14f, 14f, 42f, 70f };
        for (int i = 0; i < xs.Length - 1; i++)
        {
            float x1 = xs[i];
            float x2 = xs[i + 1];
            float z1 = CreekZAt(x1);
            float z2 = CreekZAt(x2);
            Vector3 mid = new Vector3((x1 + x2) * 0.5f, 0f, (z1 + z2) * 0.5f);
            mid.y = GroundY(terrain, mid) + 0.08f;

            float dx = x2 - x1;
            float dz = z2 - z1;
            float length = Mathf.Sqrt(dx * dx + dz * dz) + 1f;
            float yaw = -Mathf.Atan2(dz, dx) * Mathf.Rad2Deg;

            GameObject waterObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterObject.name = "Creek_" + i;
            waterObject.transform.SetParent(creekRoot, false);
            waterObject.transform.position = mid;
            waterObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            waterObject.transform.localScale = new Vector3(length, 0.06f, 6.5f);
            waterObject.GetComponent<Renderer>().sharedMaterial = water;
            UnityEngine.Object.DestroyImmediate(waterObject.GetComponent<Collider>());
        }

        // Polytope 나무 다리 1개.
        PlaceAtGround(terrain, bridge, BridgeCenter + new Vector3(0f, 0.18f, 0f), Quaternion.Euler(0f, 90f, 0f), propsRoot, "WoodenBridge", 1.00f);

        // 강변 장식도 최소화.
        Vector3[] bankSpots =
        {
            new Vector3(-30f,0f, CreekZAt(-30f)-5f),
            new Vector3(27f,0f, CreekZAt(27f)+5f),
            new Vector3(-48f,0f, CreekZAt(-48f)+5f),
            new Vector3(47f,0f, CreekZAt(47f)-5f)
        };
        for (int i = 0; i < bankSpots.Length; i++)
        {
            PlaceAtGround(terrain, riverRock, bankSpots[i], RandomYaw(), creekRoot, "CreekRock_" + i, 0.85f);
            if (i < 2)
                PlaceAtGround(terrain, grass, bankSpots[i] + new Vector3(2f,0f,1f), RandomYaw(), creekRoot, "CreekGrass_" + i, 0.95f);
            else
                PlaceAtGround(terrain, poppy, bankSpots[i] + new Vector3(-2f,0f,-1f), RandomYaw(), creekRoot, "CreekFlower_" + i, 0.95f);
        }
    }

    private static void BuildPortal(Terrain terrain, Transform parent, GameObject menhir, GameObject rock)
    {
        Material portalMaterial = CreatePortalMaterial();
        Vector3 center = OnGround(terrain, PortalCenter);

        PlaceAtGround(terrain, menhir != null ? menhir : rock, center + new Vector3(-3.0f,0f,0f), Quaternion.Euler(0f, 8f, 0f), parent, "PortalStone_L", 1.05f);
        PlaceAtGround(terrain, menhir != null ? menhir : rock, center + new Vector3(3.0f,0f,0f), Quaternion.Euler(0f, -8f, 0f), parent, "PortalStone_R", 1.05f);

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "DungeonPortal_Visual";
        orb.transform.SetParent(parent, false);
        orb.transform.position = center + new Vector3(0f, 2.4f, 0f);
        orb.transform.localScale = new Vector3(2.9f, 3.6f, 0.25f);
        orb.GetComponent<Renderer>().sharedMaterial = portalMaterial;
        UnityEngine.Object.DestroyImmediate(orb.GetComponent<Collider>());

        GameObject trigger = new GameObject("DungeonPortalTrigger");
        trigger.transform.SetParent(parent, false);
        trigger.transform.position = center + new Vector3(0f, 2.2f, 0f);
        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(4.5f, 4.8f, 2.0f);
    }

    private static void BuildLighting(Transform parent, Material skybox)
    {
        if (skybox != null)
            RenderSettings.skybox = skybox;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.95f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.78f, 0.88f, 0.93f, 1f);
        RenderSettings.fogDensity = 0.0016f;

        GameObject sunObject = new GameObject("Directional Light");
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.95f, 0.86f);
        sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft;
    }

    private static void BuildMarkers(Terrain terrain, Transform parent)
    {
        CreateMarker("PlayerSpawn", OnGround(terrain, SpawnPoint), parent);
        CreateMarker("VillageCenter", OnGround(terrain, VillageCenter), parent);
        CreateMarker("NPC_Spawn_01", OnGround(terrain, new Vector3(-5f,0f,4f)), parent);
        CreateMarker("NPC_Spawn_02", OnGround(terrain, new Vector3(5f,0f,6f)), parent);
        CreateMarker("NPC_Spawn_03", OnGround(terrain, new Vector3(0f,0f,13f)), parent);
        CreateMarker("BridgePoint", OnGround(terrain, BridgeCenter), parent);
        CreateMarker("PortalPoint", OnGround(terrain, PortalCenter), parent);
    }

    private static void BuildPreviewCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("HubPreviewCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(42f, 42f, -62f);
        Vector3 target = VillageCenter + new Vector3(0f, 4f, 8f);
        cameraObject.transform.rotation = Quaternion.LookRotation(target - cameraObject.transform.position, Vector3.up);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 220f;
        camera.clearFlags = CameraClearFlags.Skybox;
    }

    // ---------- 성능 친화적인 배치 로직 ----------

    private static void Scatter(Terrain terrain, Transform parent, GameObject[] choices, int count,
        float minX, float maxX, float minZ, float maxZ, Func<Vector3, bool> predicate,
        float minScale, float maxScale, string prefix)
    {
        List<GameObject> valid = choices.Where(x => x != null).Distinct().ToList();
        if (valid.Count == 0) return;

        int placed = 0;
        int attempts = 0;
        while (placed < count && attempts < count * 40)
        {
            attempts++;
            Vector3 p = new Vector3(
                Mathf.Lerp(minX, maxX, (float)rng.NextDouble()),
                0f,
                Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble()));

            if (!predicate(p)) continue;

            GameObject prefab = valid[rng.Next(valid.Count)];
            float scale = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
            PlaceAtGround(terrain, prefab, p, RandomYaw(), parent, prefix + "_" + placed.ToString("00"), scale);
            placed++;
        }
    }

    private static bool IsOuterTreeSpot(Vector3 p)
    {
        if (!InsideTerrain(p)) return false;
        float village = Vector2.Distance(new Vector2(p.x,p.z), new Vector2(VillageCenter.x,VillageCenter.z));
        if (village < 29f) return false;
        if (DistanceToMainPath(p.x, p.z) < 7f) return false;
        if (Mathf.Abs(p.z - CreekZAt(p.x)) < 7f) return false;
        return Mathf.Abs(p.x) > 37f || Mathf.Abs(p.z) > 42f;
    }

    private static bool IsMidTreeSpot(Vector3 p)
    {
        if (!InsideTerrain(p)) return false;
        float village = Vector2.Distance(new Vector2(p.x,p.z), new Vector2(VillageCenter.x,VillageCenter.z));
        if (village < 25f) return false;
        if (DistanceToMainPath(p.x, p.z) < 8f) return false;
        if (Mathf.Abs(p.z - CreekZAt(p.x)) < 7f) return false;
        return true;
    }

    private static bool IsSmallNatureSpot(Vector3 p)
    {
        if (!InsideTerrain(p)) return false;
        if (DistanceToMainPath(p.x,p.z) < 4f) return false;
        if (Vector2.Distance(new Vector2(p.x,p.z), new Vector2(VillageCenter.x,VillageCenter.z)) < 18f) return false;
        if (Mathf.Abs(p.z - CreekZAt(p.x)) < 4f) return false;
        return true;
    }

    private static bool IsGrassSpot(Vector3 p)
    {
        if (!InsideTerrain(p)) return false;
        if (DistanceToMainPath(p.x,p.z) < 2.8f) return false;
        if (Vector2.Distance(new Vector2(p.x,p.z), new Vector2(VillageCenter.x,VillageCenter.z)) < 13f) return false;
        return true;
    }

    private static bool IsRockSpot(Vector3 p)
    {
        if (!InsideTerrain(p)) return false;
        if (DistanceToMainPath(p.x,p.z) < 4f) return false;
        if (Vector2.Distance(new Vector2(p.x,p.z), new Vector2(VillageCenter.x,VillageCenter.z)) < 20f) return false;
        return true;
    }

    // ---------- 길 / 지형 수학 ----------

    private static Vector3 MainPathPoint(float t)
    {
        Vector3 a = SpawnPoint;
        Vector3 b = new Vector3(-10f, 0f, -38f);
        Vector3 c = new Vector3(8f, 0f, -23f);
        Vector3 d = new Vector3(0f, 0f, -15f);
        return CubicBezier(a,b,c,d,Mathf.Clamp01(t));
    }

    private static float DistanceToMainPath(float x, float z)
    {
        Vector2 p = new Vector2(x,z);
        float best = float.MaxValue;
        Vector3 previous = MainPathPoint(0f);

        for (int i = 1; i <= 20; i++)
        {
            Vector3 current = MainPathPoint(i / 20f);
            best = Mathf.Min(best, DistanceToSegment(p, new Vector2(previous.x,previous.z), new Vector2(current.x,current.z)));
            previous = current;
        }

        // 마을 -> 다리 -> 포탈 길
        best = Mathf.Min(best, DistanceToSegment(p, new Vector2(0f,12f), new Vector2(BridgeCenter.x,BridgeCenter.z)));
        best = Mathf.Min(best, DistanceToSegment(p, new Vector2(BridgeCenter.x,BridgeCenter.z), new Vector2(PortalCenter.x,PortalCenter.z)));
        return best;
    }

    private static float CreekZAt(float x)
    {
        return 31f + Mathf.Sin(x * 0.055f) * 2.2f;
    }

    private static Vector3 CubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        return u*u*u*a + 3f*u*u*t*b + 3f*u*t*t*c + t*t*t*d;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float length = ab.sqrMagnitude;
        if (length < 0.0001f) return Vector2.Distance(p,a);
        float t = Mathf.Clamp01(Vector2.Dot(p-a,ab) / length);
        return Vector2.Distance(p, a + ab*t);
    }

    private static float Smooth01(float a, float b, float value)
    {
        return Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(a,b,value));
    }

    // ---------- 에셋 / 머티리얼 ----------

    private static Material PrepareRpgppMaterial()
    {
        Material material = FindMaterial("rpgpp_lt_mat_a", "RPGPP_LT");
        Texture2D texture = FindTexture("rpgpp_lt_tex_a", "RPGPP_LT");
        if (material == null) return null;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) material.shader = shader;

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyRpgppMaterial(GameObject go)
    {
        if (go == null || rpgppMaterial == null) return;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            int count = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials.Length : 1;
            Material[] materials = new Material[count];
            for (int i = 0; i < count; i++) materials[i] = rpgppMaterial;
            renderer.sharedMaterials = materials;
        }
    }

    private static Material CreateWaterMaterial(Material source)
    {
        DeleteIfExists(WaterMaterialPath);
        Material material;
        if (source != null)
            material = new Material(source);
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            Color color = new Color(0.35f,0.68f,0.86f,1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
            material.color = color;
        }
        AssetDatabase.CreateAsset(material, WaterMaterialPath);
        return material;
    }

    private static Material CreatePortalMaterial()
    {
        DeleteIfExists(PortalMaterialPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        Color color = new Color(0.10f,0.50f,1.00f,1f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        material.color = color;
        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",color*3f);
        AssetDatabase.CreateAsset(material, PortalMaterialPath);
        return material;
    }

    private static void PlaceTintedTree(Terrain terrain, GameObject prefab, Vector3 position, Transform parent, string name, Color tint, float scale)
    {
        GameObject go = PlaceAtGround(terrain,prefab,position,RandomYaw(),parent,name,scale);
        if (go == null) return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            // IMPORTANT: edit mode에서 renderer.material / renderer.materials를 호출하면
            // Unity가 임시 머티리얼 인스턴스를 만들어 경고/에러 로그를 남긴다.
            // sharedMaterials를 읽고, 색을 바꿀 슬롯만 명시적으로 복제해서 다시 할당한다.
            Material[] sourceMaterials = renderer.sharedMaterials;
            if (sourceMaterials == null || sourceMaterials.Length == 0) continue;

            Material[] materials = new Material[sourceMaterials.Length];
            bool changed = false;

            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                materials[i] = source;
                if (source == null) continue;

                string lower = source.name.ToLowerInvariant();
                if (lower.Contains("trunk") || lower.Contains("bark") || lower.Contains("wood"))
                    continue;

                Material material = new Material(source);
                material.name = source.name + "_Tinted_" + name;

                if (material.HasProperty("_CUSTOMCOLORSTINTING")) material.SetFloat("_CUSTOMCOLORSTINTING",1f);
                if (material.HasProperty("_GroundColor")) material.SetColor("_GroundColor",Color.Lerp(tint,new Color(0.22f,0.08f,0.04f,1f),0.20f));
                if (material.HasProperty("_TopColor")) material.SetColor("_TopColor",Color.Lerp(tint,Color.white,0.10f));
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",tint);
                if (material.HasProperty("_Color")) material.SetColor("_Color",tint);

                materials[i] = material;
                changed = true;
            }

            if (changed) renderer.sharedMaterials = materials;
        }
    }

    private static void DisableCollidersByName(Transform root, string[] tokens)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            string name = collider.gameObject.name;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (name.IndexOf(tokens[i],StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    collider.enabled = false;
                    break;
                }
            }
        }
    }

    // ---------- 공통 헬퍼 ----------

    private static GameObject PlaceAtGround(Terrain terrain, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, string name, float scale = 1f)
    {
        if (prefab == null) return null;
        position.y = GroundY(terrain,position);
        return PlacePrefab(prefab,position,rotation,parent,name,scale);
    }

    private static GameObject PlacePrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, string name, float scale)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (go == null) return null;
        go.transform.SetParent(parent,true);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = go.transform.localScale * scale;
        go.name = name;
        return go;
    }

    private static float GroundY(Terrain terrain, Vector3 position)
    {
        return terrain.SampleHeight(position) + terrain.transform.position.y;
    }

    private static Vector3 OnGround(Terrain terrain, Vector3 position)
    {
        position.y = GroundY(terrain,position) + 0.05f;
        return position;
    }

    private static bool InsideTerrain(Vector3 p)
    {
        return p.x > -71f && p.x < 71f && p.z > -71f && p.z < 71f;
    }

    private static Quaternion FaceToward(Vector3 from, Vector3 target)
    {
        Vector3 direction = target - from;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return Quaternion.identity;
        return Quaternion.LookRotation(direction.normalized,Vector3.up);
    }

    private static Quaternion RandomYaw()
    {
        return Quaternion.Euler(0f,(float)rng.NextDouble()*360f,0f);
    }

    private static Transform MakeGroup(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent,false);
        return go.transform;
    }

    private static void CreateMarker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent,false);
        marker.transform.position = position;
    }

    private static GameObject FindPrefab(string assetName, string preferredPath)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:Prefab");
        if (guids == null || guids.Length == 0) return null;

        string target = assetName.ToLowerInvariant();
        string preferred = preferredPath.ToLowerInvariant();
        List<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToList();

        string path = paths.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p).ToLowerInvariant() == target &&
            p.Replace("\\","/").ToLowerInvariant().Contains(preferred));

        if (string.IsNullOrEmpty(path))
            path = paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant() == target);

        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Texture2D FindTexture(string assetName, string preferredPath)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:Texture2D");
        if (guids == null || guids.Length == 0) return null;
        string preferred = preferredPath.ToLowerInvariant();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Replace("\\","/").ToLowerInvariant().Contains(preferred))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static Material FindMaterial(string assetName, string preferredPath)
    {
        string[] guids = AssetDatabase.FindAssets(assetName + " t:Material");
        if (guids == null || guids.Length == 0) return null;
        string preferred = preferredPath.ToLowerInvariant();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Replace("\\","/").ToLowerInvariant().Contains(preferred))
                return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
        return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void EnsureFolder(string folder)
    {
        string normalized = folder.Replace("\\","/");
        if (AssetDatabase.IsValidFolder(normalized)) return;

        string[] parts = normalized.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current,parts[i]);
            current = next;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
#endif
