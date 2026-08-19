#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class NGonForgeCompactDungeonGenerator
{
    private const string RootPath = "Assets/Dungeon/URP/Prefabs";
    private const string SceneOutputPath = "Assets/Scenes/NGF_CompactDungeon.unity";

    private static System.Random rng = new System.Random(1207);

    private class PInfo
    {
        public string path;
        public GameObject prefab;
        public Vector3 size;
    }

    [MenuItem("Tools/N-GonForge/Generate Compact Styled Dungeon")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(RootPath))
        {
            EditorUtility.DisplayDialog(
                "URP 프리팹 경로를 찾지 못했습니다",
                RootPath + "\n경로가 존재하는지 확인해주세요.",
                "확인");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets/Scenes");

        var all = LoadCatalog();
        if (all.Count == 0)
        {
            EditorUtility.DisplayDialog("프리팹 없음", "URP 프리팹을 찾지 못했습니다.", "확인");
            return;
        }

        // ---- Curated picks with fallback ----
        var floorMain = PickFirst(all,
            "URP/Prefabs/Environment/Floor/NGF_Env_Floor_01.prefab",
            "URP/Prefabs/Environment/Floor/NGF_Env_Floor_02.prefab",
            contains: new []{ "/environment/floor/" });

        var floorAlt = PickFirst(all,
            "URP/Prefabs/Environment/Floor/NGF_Env_Floor_08.prefab",
            "URP/Prefabs/Environment/Floor/NGF_Env_Floor_12.prefab",
            contains: new []{ "/environment/floor/" },
            exclude: new []{ "collision" });

        var brokenFloor = PickFirst(all,
            "URP/Prefabs/Environment/Broken floor/NGF_Env_Broken_floor_03.prefab",
            contains: new []{ "/broken floor/" });

        var wallMain = PickFirst(all,
            "URP/Prefabs/Environment/Wall/NGF_Env_Wall_01.prefab",
            "URP/Prefabs/Environment/Wall/NGF_Env_Wall_03.prefab",
            contains: new []{ "/environment/wall/" });

        var wallAlt = PickFirst(all,
            "URP/Prefabs/Environment/Wall/NGF_Env_Wall_08.prefab",
            "URP/Prefabs/Environment/Wall/NGF_Env_Wall_10.prefab",
            contains: new []{ "/environment/wall/" });

        var doorway = PickFirst(all,
            "URP/Prefabs/Environment/Doorway/NGF_Env_Stone_doorway_01.prefab",
            "URP/Prefabs/Environment/Doorway/NGF_Env_Stone_doorway_02.prefab",
            contains: new []{ "/environment/doorway/" });

        var column = PickFirst(all,
            "URP/Prefabs/Environment/Column/NGF_Env_Column_01.prefab",
            contains: new []{ "/environment/column/" });

        var stair = PickFirst(all,
            "URP/Prefabs/Environment/Stairs/NGF_Env_Stairs_01.prefab",
            contains: new []{ "/environment/stairs/" });

        var corner = PickFirst(all,
            "URP/Prefabs/Environment/Stone corner/NGF_Env_Stone_corner_01.prefab",
            contains: new []{ "/stone corner/" });

        var boardLong = PickFirst(all,
            "URP/Prefabs/Environment/Wooden boards and bars/NGF_Env_Long_board_01.prefab",
            contains: new []{ "long_board" });

        // Furniture
        var tableLarge = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Table_02.prefab", contains: new []{ "table" });
        var tableSmall = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Table_01.prefab", contains: new []{ "table" });
        var chairA = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Chair_01.prefab", contains: new []{ "chair" });
        var chairB = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Chair_02.prefab", contains: new []{ "chair" });
        var bench = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Bench.prefab", contains: new []{ "bench" });
        var rack = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Rack_01.prefab", contains: new []{ "rack" });
        var shelf = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Shelf_01.prefab", contains: new []{ "shelf" });
        var stool = PickFirst(all, "URP/Prefabs/Misc/Furniture/NGF_Stool_01.prefab", contains: new []{ "stool" });

        // Props / items
        var barrel = PickFirst(all, "URP/Prefabs/Misc/Props/NGF_Barrel_01.prefab", contains: new []{ "barrel" });
        var crate = PickFirst(all, "URP/Prefabs/Misc/Props/NGF_Crate_01.prefab", contains: new []{ "crate" });
        var chest = PickFirst(all, "URP/Prefabs/Misc/Props/NGF_Chest_01.prefab", contains: new []{ "chest" });
        var chestBig = PickFirst(all, "URP/Prefabs/Misc/Props/NGF_Chest_02.prefab", contains: new []{ "chest" });
        var bottle = PickFirst(all, "URP/Prefabs/Misc/Item/Bottles Jugs Cups/NGF_Bottle_01.prefab", contains: new []{ "bottle" });
        var jug = PickFirst(all, "URP/Prefabs/Misc/Item/Bottles Jugs Cups/NGF_Jug_01.prefab", contains: new []{ "jug" });
        var mug = PickFirst(all, "URP/Prefabs/Misc/Item/Bottles Jugs Cups/NGF_Mug.prefab", contains: new []{ "mug" });
        var book = PickFirst(all, "URP/Prefabs/Misc/Item/Books/NGF_Book_01.prefab", contains: new []{ "book" });
        var bookOpen = PickFirst(all, "URP/Prefabs/Misc/Item/Books/NGF_Book_open_01.prefab", contains: new []{ "book_open" });
        var coins = PickFirst(all, "URP/Prefabs/Misc/Item/NGF_Coins_01.prefab", contains: new []{ "coins" });
        var pouch = PickFirst(all, "URP/Prefabs/Misc/Item/NGF_Pouch_01.prefab", contains: new []{ "pouch" });
        var key = PickFirst(all, "URP/Prefabs/Misc/Item/NGF_Key_01.prefab", contains: new []{ "key" });
        var paper = PickFirst(all, "URP/Prefabs/Misc/Item/NGF_Sheet_01.prefab", contains: new []{ "sheet" });
        var dustPile = PickFirst(all, "URP/Prefabs/Misc/Item/NGF_Pile_of_dust_01.prefab", contains: new []{ "pile_of_dust" });

        // Lights
        var torch = PickFirst(all, "URP/Prefabs/Misc/Light/NGF_Light_Torch_01.prefab", contains: new []{ "torch" });
        var campfire = PickFirst(all, "URP/Prefabs/Misc/Light/NGF_Light_Campfire_01.prefab", contains: new []{ "campfire" });
        var pedestal = PickFirst(all, "URP/Prefabs/Misc/Light/NGF_Light_Pedestal_01.prefab", contains: new []{ "pedestal" });
        var crystal = PickFirst(all, "URP/Prefabs/Misc/Light/Crystal/NGF_Light_Crystal_group_small_01.prefab", contains: new []{ "crystal_group_small" });

        if (floorMain == null || wallMain == null)
        {
            EditorUtility.DisplayDialog("핵심 프리팹 누락", "Floor 또는 Wall 프리팹을 찾지 못했습니다.", "확인");
            return;
        }

        // Determine grid size from chosen floor
        float cellX = ClampCell(floorMain.size.x);
        float cellZ = ClampCell(floorMain.size.z);
        float wallHeight = Mathf.Max(2.6f, wallMain.size.y);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("NGF_CompactDungeon");
        var arch = CreateRoot("01_Architecture", root.transform);
        var furn = CreateRoot("02_Furniture", root.transform);
        var props = CreateRoot("03_Props_Items", root.transform);
        var deco = CreateRoot("04_Decor", root.transform);
        var lights = CreateRoot("05_Lighting", root.transform);
        var markers = CreateRoot("06_GameplayMarkers", root.transform);

        // Layout: compact but readable
        // Entrance 3x3, hall, main 4x4, side treasury 3x3, boss 4x4
        var cells = new HashSet<Vector2Int>();
        AddRect(cells, -1, 1, 0, 2);      // entrance
        AddRect(cells, 0, 0, -2, -1);     // hall
        AddRect(cells, -1, 2, -6, -3);    // main room
        AddRect(cells, 3, 5, -5, -3);     // treasury branch
        AddRect(cells, 0, 0, -8, -7);     // boss hall
        AddRect(cells, -1, 2, -12, -9);   // boss room

        // Place floors
        foreach (var c in cells)
        {
            var f = floorMain;
            if (brokenFloor != null && (Math.Abs(c.x + c.y) % 11 == 2))
                f = brokenFloor;
            else if (floorAlt != null && (Math.Abs(c.x * 3 + c.y) % 7 == 1))
                f = floorAlt;

            Place(f, Cell(c, cellX, cellZ), Quaternion.identity, arch, $"Floor_{c.x}_{c.y}");
        }

        // Walls around bounds
        foreach (var c in cells)
        {
            TryWall(c, Vector2Int.up, cells, PickWallVariant(c, wallMain, wallAlt), arch, cellX, cellZ);
            TryWall(c, Vector2Int.down, cells, PickWallVariant(c, wallMain, wallAlt), arch, cellX, cellZ);
            TryWall(c, Vector2Int.right, cells, PickWallVariant(c, wallMain, wallAlt), arch, cellX, cellZ);
            TryWall(c, Vector2Int.left, cells, PickWallVariant(c, wallMain, wallAlt), arch, cellX, cellZ);
        }

        // Transition doorways
        if (doorway != null)
        {
            Place(doorway, Cell(new Vector2Int(0, -2), cellX, cellZ), Quaternion.identity, arch, "Doorway_EntranceToMain");
            Place(doorway, Cell(new Vector2Int(3, -4), cellX, cellZ), Quaternion.Euler(0f, 90f, 0f), arch, "Doorway_ToTreasury");
            Place(doorway, Cell(new Vector2Int(0, -8), cellX, cellZ), Quaternion.identity, arch, "Doorway_ToBoss");
        }

        // Columns main room + boss room
        if (column != null)
        {
            var colPts = new []
            {
                new Vector2Int(-1,-3), new Vector2Int(2,-3), new Vector2Int(-1,-6), new Vector2Int(2,-6),
                new Vector2Int(-1,-9), new Vector2Int(2,-9), new Vector2Int(-1,-12), new Vector2Int(2,-12)
            };
            int idx = 0;
            foreach (var p in colPts)
                Place(column, Cell(p, cellX, cellZ), Quaternion.identity, arch, $"Column_{idx++:00}");
        }

        // Corners decorative in boss room if available
        if (corner != null)
        {
            Place(corner, Cell(new Vector2Int(-1,-9), cellX, cellZ), Quaternion.Euler(0, 0, 0), arch, "Corner_Boss_00");
            Place(corner, Cell(new Vector2Int(2,-9), cellX, cellZ), Quaternion.Euler(0, 90, 0), arch, "Corner_Boss_01");
            Place(corner, Cell(new Vector2Int(-1,-12), cellX, cellZ), Quaternion.Euler(0, -90, 0), arch, "Corner_Boss_02");
            Place(corner, Cell(new Vector2Int(2,-12), cellX, cellZ), Quaternion.Euler(0, 180, 0), arch, "Corner_Boss_03");
        }

        // A small staircase setpiece at the entry of boss hall
        if (stair != null)
        {
            Place(stair, Cell(new Vector2Int(0, -8), cellX, cellZ) + new Vector3(0f, 0f, cellZ * 0.7f), Quaternion.identity, deco, "BossHall_Stairs");
        }

        // Optional boards for atmosphere
        if (boardLong != null)
        {
            Place(boardLong, Cell(new Vector2Int(1, -4), cellX, cellZ) + new Vector3(0.15f, 0f, 0.2f), Quaternion.Euler(0f, 20f, 0f), deco, "Board_Decor_01");
            Place(boardLong, Cell(new Vector2Int(4, -4), cellX, cellZ) + new Vector3(-0.15f, 0f, -0.2f), Quaternion.Euler(0f, -35f, 0f), deco, "Board_Decor_02");
        }

        // -------- Entrance room dressing --------
        var entranceCenter = RoomCenter(-1,1,0,2,cellX,cellZ);
        if (tableSmall != null) Place(tableSmall, entranceCenter + new Vector3(0f,0f,0.2f), Quaternion.identity, furn, "Entrance_Table");
        if (chairA != null)
        {
            Place(chairA, entranceCenter + new Vector3(-0.7f,0f,0.9f), Quaternion.Euler(0,135,0), furn, "Entrance_ChairA");
            Place(chairA, entranceCenter + new Vector3(0.85f,0f,-0.75f), Quaternion.Euler(0,-45,0), furn, "Entrance_ChairB");
        }
        if (rack != null) Place(rack, Cell(new Vector2Int(-1,2), cellX, cellZ) + new Vector3(-0.5f,0f,0.2f), Quaternion.Euler(0,90,0), furn, "Entrance_Rack");
        if (barrel != null) Place(barrel, Cell(new Vector2Int(1,0), cellX, cellZ) + new Vector3(0.35f,0f,-0.2f), Quaternion.identity, props, "Entrance_Barrel");
        if (bookOpen != null) Place(bookOpen, entranceCenter + new Vector3(0.1f,0.83f,0.1f), Quaternion.Euler(0,-10,0), props, "Entrance_Book");
        if (bottle != null) Place(bottle, entranceCenter + new Vector3(-0.18f,0.83f,-0.1f), Quaternion.identity, props, "Entrance_Bottle");
        if (paper != null) Place(paper, entranceCenter + new Vector3(0.18f,0.83f,-0.15f), Quaternion.Euler(0,16,0), props, "Entrance_Paper");

        // -------- Main room dressing --------
        var mainCenter = RoomCenter(-1,2,-6,-3,cellX,cellZ);
        if (tableLarge != null) Place(tableLarge, mainCenter + new Vector3(0.15f,0f,-0.15f), Quaternion.Euler(0,90,0), furn, "Main_Table");
        if (bench != null)
        {
            Place(bench, mainCenter + new Vector3(-1.2f,0f,-0.95f), Quaternion.Euler(0,90,0), furn, "Main_BenchA");
            Place(bench, mainCenter + new Vector3(1.45f,0f,0.95f), Quaternion.Euler(0,-90,0), furn, "Main_BenchB");
        }
        if (stool != null)
        {
            Place(stool, mainCenter + new Vector3(0.75f,0f,1.2f), Quaternion.identity, furn, "Main_StoolA");
            Place(stool, mainCenter + new Vector3(-0.95f,0f,-1.25f), Quaternion.identity, furn, "Main_StoolB");
        }
        if (barrel != null)
        {
            Place(barrel, Cell(new Vector2Int(-1,-6), cellX, cellZ) + new Vector3(-0.4f,0f,-0.4f), Quaternion.identity, props, "Main_BarrelA");
            Place(barrel, Cell(new Vector2Int(2,-3), cellX, cellZ) + new Vector3(0.4f,0f,0.35f), Quaternion.identity, props, "Main_BarrelB");
        }
        if (crate != null)
        {
            Place(crate, Cell(new Vector2Int(-1,-3), cellX, cellZ) + new Vector3(-0.1f,0f,0.4f), Quaternion.identity, props, "Main_CrateA");
            Place(crate, Cell(new Vector2Int(2,-6), cellX, cellZ) + new Vector3(0.1f,0f,-0.3f), Quaternion.Euler(0,22,0), props, "Main_CrateB");
        }
        PlaceSmallTableSet(props, book, mug, jug, mainCenter + new Vector3(0f,0.86f,-0.15f));

        // -------- Treasury room --------
        var treasuryCenter = RoomCenter(3,5,-5,-3,cellX,cellZ);
        if (shelf != null) Place(shelf, Cell(new Vector2Int(5,-3), cellX, cellZ) + new Vector3(0.35f,0f,0.35f), Quaternion.Euler(0,180,0), furn, "Treasury_Shelf");
        if (tableSmall != null) Place(tableSmall, treasuryCenter + new Vector3(-0.3f,0f,0.15f), Quaternion.Euler(0,90,0), furn, "Treasury_Table");
        if (chestBig != null) Place(chestBig, Cell(new Vector2Int(5,-5), cellX, cellZ) + new Vector3(0.1f,0f,-0.15f), Quaternion.Euler(0,-25,0), props, "Treasury_Chest");
        if (chest != null) Place(chest, Cell(new Vector2Int(3,-3), cellX, cellZ) + new Vector3(-0.25f,0f,0.15f), Quaternion.Euler(0,15,0), props, "Treasury_ChestSmall");
        if (coins != null)
        {
            Place(coins, treasuryCenter + new Vector3(0.12f,0.82f,0.15f), Quaternion.identity, props, "Treasury_Coins");
            Place(coins, Cell(new Vector2Int(5,-5), cellX, cellZ) + new Vector3(0.35f,0.45f,-0.15f), Quaternion.identity, props, "Treasury_CoinsChest");
        }
        if (pouch != null) Place(pouch, treasuryCenter + new Vector3(-0.12f,0.82f,-0.1f), Quaternion.Euler(0,35,0), props, "Treasury_Pouch");
        if (key != null) Place(key, treasuryCenter + new Vector3(0.28f,0.82f,-0.05f), Quaternion.Euler(0,10,0), props, "Treasury_Key");
        if (crystal != null) Place(crystal, Cell(new Vector2Int(4,-5), cellX, cellZ) + new Vector3(0f,0f,0.3f), Quaternion.identity, deco, "Treasury_Crystal");

        // -------- Boss room --------
        var bossCenter = RoomCenter(-1,2,-12,-9,cellX,cellZ);
        if (campfire != null) Place(campfire, bossCenter, Quaternion.identity, deco, "Boss_Campfire");
        if (rack != null)
        {
            Place(rack, Cell(new Vector2Int(-1,-9), cellX, cellZ) + new Vector3(-0.25f,0f,0.2f), Quaternion.Euler(0,90,0), furn, "Boss_RackA");
            Place(rack, Cell(new Vector2Int(2,-12), cellX, cellZ) + new Vector3(0.2f,0f,-0.25f), Quaternion.Euler(0,-90,0), furn, "Boss_RackB");
        }
        if (barrel != null)
        {
            Place(barrel, Cell(new Vector2Int(-1,-12), cellX, cellZ) + new Vector3(-0.35f,0f,-0.25f), Quaternion.identity, props, "Boss_BarrelA");
            Place(barrel, Cell(new Vector2Int(2,-9), cellX, cellZ) + new Vector3(0.35f,0f,0.25f), Quaternion.identity, props, "Boss_BarrelB");
        }
        if (crate != null)
        {
            Place(crate, Cell(new Vector2Int(2,-12), cellX, cellZ) + new Vector3(0.2f,0f,-0.45f), Quaternion.Euler(0,18,0), props, "Boss_Crate");
        }
        if (dustPile != null)
        {
            Place(dustPile, bossCenter + new Vector3(1.1f,0.02f,0.65f), Quaternion.identity, props, "Boss_DustA");
            Place(dustPile, bossCenter + new Vector3(-1.0f,0.02f,-0.7f), Quaternion.identity, props, "Boss_DustB");
        }

        // Light props + manually tuned point lights
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(-1,2), cellX, cellZ), Cell(new Vector2Int(1,2), cellX, cellZ), "Entrance");
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(-1,-3), cellX, cellZ), Cell(new Vector2Int(2,-3), cellX, cellZ), "MainNorth");
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(-1,-6), cellX, cellZ), Cell(new Vector2Int(2,-6), cellX, cellZ), "MainSouth");
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(3,-3), cellX, cellZ), Cell(new Vector2Int(5,-5), cellX, cellZ), "Treasury");
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(-1,-9), cellX, cellZ), Cell(new Vector2Int(2,-9), cellX, cellZ), "BossNorth");
        AddTorchPair(torch, lights, deco, Cell(new Vector2Int(-1,-12), cellX, cellZ), Cell(new Vector2Int(2,-12), cellX, cellZ), "BossSouth");

        if (pedestal != null)
        {
            Place(pedestal, bossCenter + new Vector3(-1.4f, 0f, 0f), Quaternion.identity, deco, "Boss_PedestalA");
            Place(pedestal, bossCenter + new Vector3(1.4f, 0f, 0f), Quaternion.identity, deco, "Boss_PedestalB");
            AddPointLight(bossCenter + new Vector3(-1.4f, 1.4f, 0f), lights, "BossPedestalLightA", new Color(1f,0.42f,0.18f), 1.25f, 6f, false);
            AddPointLight(bossCenter + new Vector3(1.4f, 1.4f, 0f), lights, "BossPedestalLightB", new Color(1f,0.42f,0.18f), 1.25f, 6f, false);
        }

        // Main ambience lights
        AddPointLight(entranceCenter + new Vector3(0f, 2.2f, 0f), lights, "EntranceFill", new Color(1f,0.55f,0.28f), 0.9f, 7f, true);
        AddPointLight(mainCenter + new Vector3(0f, 2.3f, 0f), lights, "MainFill", new Color(1f,0.56f,0.30f), 0.75f, 8f, false);
        AddPointLight(treasuryCenter + new Vector3(0f, 2.1f, 0f), lights, "TreasuryFill", new Color(0.45f,0.72f,1f), 0.55f, 5.5f, false);
        AddPointLight(bossCenter + new Vector3(0f, 2.3f, 0f), lights, "BossFill", new Color(1f,0.45f,0.20f), 1.15f, 8f, true);

        // Render settings
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.09f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.045f, 1f);
        RenderSettings.fogDensity = 0.0055f;

        var dir = new GameObject("MoonFill");
        dir.transform.SetParent(lights, false);
        dir.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        var dl = dir.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 0.18f;
        dl.color = new Color(0.50f, 0.56f, 0.72f);
        dl.shadows = LightShadows.None;

        // Markers
        var pRoot = CreateRoot("Player", markers);
        var eRoot = CreateRoot("Enemies", markers);
        var oRoot = CreateRoot("Objectives", markers);
        CreateMarker("PlayerSpawn", entranceCenter + new Vector3(0f, 0.1f, 0.8f), pRoot);
        CreateMarker("Enemy_Main_01", mainCenter + new Vector3(-1.1f, 0.1f, -0.8f), eRoot);
        CreateMarker("Enemy_Main_02", mainCenter + new Vector3(1.0f, 0.1f, 0.9f), eRoot);
        CreateMarker("Enemy_Main_03", mainCenter + new Vector3(0.8f, 0.1f, -1.0f), eRoot);
        CreateMarker("Enemy_Treasury_01", treasuryCenter + new Vector3(0.0f, 0.1f, 0.6f), eRoot);
        CreateMarker("BossSpawn", bossCenter + new Vector3(0f, 0.1f, 0f), eRoot);
        CreateMarker("TreasurePoint", treasuryCenter + new Vector3(0f, 0.1f, 0f), oRoot);
        CreateMarker("ExitPoint", Cell(new Vector2Int(0,2), cellX, cellZ) + new Vector3(0f,0.1f,-0.85f), oRoot);

        // Camera
        var camGo = new GameObject("MapPreviewCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(root.transform, false);
        camGo.transform.position = new Vector3(cellX * 6.2f, wallHeight * 2.9f, -cellZ * 6.6f);
        camGo.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.015f, 0.018f, 0.025f);
        cam.fieldOfView = 53f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 400f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneOutputPath);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "생성 완료",
            "작은 사이즈의 N-GonForge 던전 맵을 생성했습니다.\n\n" +
            SceneOutputPath + "\n\n" +
            "결과 화면을 캡처해서 보내주면 2차 수정용으로 더 다듬어줄 수 있습니다.",
            "확인");
    }

    // ---------- Placement helpers ----------
    private static float ClampCell(float v)
    {
        if (v < 1.0f) return 2f;
        if (v > 8f) return 4f;
        return v;
    }

    private static Vector3 Cell(Vector2Int c, float cellX, float cellZ)
    {
        return new Vector3(c.x * cellX, 0f, c.y * cellZ);
    }

    private static Vector3 RoomCenter(int minX, int maxX, int minZ, int maxZ, float cellX, float cellZ)
    {
        return new Vector3(((minX + maxX) * 0.5f) * cellX, 0f, ((minZ + maxZ) * 0.5f) * cellZ);
    }

    private static void AddRect(HashSet<Vector2Int> cells, int minX, int maxX, int minZ, int maxZ)
    {
        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
                cells.Add(new Vector2Int(x, z));
    }

    private static void TryWall(Vector2Int c, Vector2Int side, HashSet<Vector2Int> cells, PInfo wall, Transform parent, float cellX, float cellZ)
    {
        if (cells.Contains(c + side) || wall == null)
            return;

        bool longOnX = wall.size.x >= wall.size.z;
        Vector3 pos = Cell(c, cellX, cellZ);
        Quaternion rot = Quaternion.identity;

        if (side == Vector2Int.up)
        {
            pos += Vector3.forward * (cellZ * 0.5f);
            rot = longOnX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        }
        else if (side == Vector2Int.down)
        {
            pos += Vector3.back * (cellZ * 0.5f);
            rot = longOnX ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.Euler(0f, -90f, 0f);
        }
        else if (side == Vector2Int.right)
        {
            pos += Vector3.right * (cellX * 0.5f);
            rot = longOnX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        }
        else
        {
            pos += Vector3.left * (cellX * 0.5f);
            rot = longOnX ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 180f, 0f);
        }

        Place(wall, pos, rot, parent, $"Wall_{c.x}_{c.y}_{side.x}_{side.y}");
    }

    private static PInfo PickWallVariant(Vector2Int c, PInfo a, PInfo b)
    {
        if (a == null) return b;
        if (b == null) return a;
        return ((Math.Abs(c.x + c.y) % 5) == 0) ? b : a;
    }

    private static void AddTorchPair(PInfo torch, Transform lightRoot, Transform decoRoot, Vector3 a, Vector3 b, string prefix)
    {
        if (torch != null)
        {
            var ga = Place(torch, a + new Vector3(0f, 0f, 0f), Quaternion.identity, decoRoot, prefix + "_TorchA");
            var gb = Place(torch, b + new Vector3(0f, 0f, 0f), Quaternion.identity, decoRoot, prefix + "_TorchB");
            DisableChildLights(ga);
            DisableChildLights(gb);
        }

        AddPointLight(a + new Vector3(0f, 1.8f, 0f), lightRoot, prefix + "_LightA", new Color(1f,0.50f,0.20f), 1.05f, 6.2f, true);
        AddPointLight(b + new Vector3(0f, 1.8f, 0f), lightRoot, prefix + "_LightB", new Color(1f,0.50f,0.20f), 1.0f, 6.2f, false);
    }

    private static void AddPointLight(Vector3 pos, Transform parent, string name, Color color, float intensity, float range, bool softShadow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = softShadow ? LightShadows.Soft : LightShadows.None;
    }

    private static void PlaceSmallTableSet(Transform parent, PInfo book, PInfo mug, PInfo jug, Vector3 center)
    {
        if (book != null) Place(book, center + new Vector3(-0.15f,0f,0.08f), Quaternion.Euler(0,-20,0), parent, "TableSet_Book");
        if (mug != null) Place(mug, center + new Vector3(0.12f,0f,-0.10f), Quaternion.identity, parent, "TableSet_Mug");
        if (jug != null) Place(jug, center + new Vector3(0.02f,0f,0.14f), Quaternion.Euler(0,18,0), parent, "TableSet_Jug");
    }

    // ---------- Asset helpers ----------
    private static List<PInfo> LoadCatalog()
    {
        var list = new List<PInfo>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { RootPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            list.Add(new PInfo { path = path, prefab = prefab, size = EstimateBounds(prefab) });
        }
        return list;
    }

    private static PInfo PickFirst(List<PInfo> all, string exact1 = null, string exact2 = null, string[] contains = null, string[] exclude = null)
    {
        string MatchExact(PInfo p)
        {
            return p.path.Replace("\\", "/");
        }

        if (!string.IsNullOrEmpty(exact1))
        {
            var p = all.FirstOrDefault(x => x.path.EndsWith(exact1, StringComparison.OrdinalIgnoreCase));
            if (p != null) return p;
        }
        if (!string.IsNullOrEmpty(exact2))
        {
            var p = all.FirstOrDefault(x => x.path.EndsWith(exact2, StringComparison.OrdinalIgnoreCase));
            if (p != null) return p;
        }

        IEnumerable<PInfo> q = all;
        if (contains != null && contains.Length > 0)
            q = q.Where(x => contains.Any(c => x.path.ToLowerInvariant().Contains(c.ToLowerInvariant())));
        if (exclude != null && exclude.Length > 0)
            q = q.Where(x => !exclude.Any(c => x.path.ToLowerInvariant().Contains(c.ToLowerInvariant())));

        return q.FirstOrDefault();
    }

    private static Vector3 EstimateBounds(GameObject prefab)
    {
        GameObject inst = null;
        try
        {
            inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (inst == null) return Vector3.one * 2f;
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.position = Vector3.zero;
            var rs = inst.GetComponentsInChildren<Renderer>(true);
            if (rs == null || rs.Length == 0) return Vector3.one * 2f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            var s = b.size;
            if (s.x < 0.01f) s.x = 1f;
            if (s.y < 0.01f) s.y = 1f;
            if (s.z < 0.01f) s.z = 1f;
            return s;
        }
        catch
        {
            return Vector3.one * 2f;
        }
        finally
        {
            if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    private static GameObject Place(PInfo info, Vector3 pos, Quaternion rot, Transform parent, string name)
    {
        if (info == null || info.prefab == null) return null;
        var go = PrefabUtility.InstantiatePrefab(info.prefab) as GameObject;
        if (go == null) return null;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.name = name;
        return go;
    }

    private static void DisableChildLights(GameObject go)
    {
        if (go == null) return;
        foreach (var l in go.GetComponentsInChildren<Light>(true))
            l.enabled = false;
    }

    private static Transform CreateRoot(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void CreateMarker(string name, Vector3 pos, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
    }

    private static void EnsureFolder(string assetFolder)
    {
        string normalized = assetFolder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalized)) return;
        string[] parts = normalized.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
