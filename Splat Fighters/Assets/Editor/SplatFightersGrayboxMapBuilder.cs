using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only utility that builds the MVP arena scene.
/// The gameplay layout keeps simple generated blockers, while imported hangar assets provide the visible environment.
/// </summary>
public static class SplatFightersGrayboxMapBuilder
{
    private const string ScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string LevelRootName = "LevelRoot";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const string HangarPrefabRoot = "Assets/Hangar Building Modular/Prefabs/";
    private const string HangarMaterialRoot = "Assets/Hangar Building Modular/Materials/";
    private const float CharacterRootHeight = 0.8f;
    private const float CharacterControllerHeight = 1.6f;
    private const float CharacterControllerRadius = 0.4f;
    private const float MapWidth = 32f;
    private const float MapLength = 36f;
    private const float ArenaContainmentHeight = 5.5f;
    private const float ArenaContainmentThickness = 0.8f;
    private const int PaintGridWidth = 80;
    private const int PaintGridHeight = 90;
    private const float HalfMapWidth = MapWidth * 0.5f;
    private const float HalfMapLength = MapLength * 0.5f;
    private static readonly List<PaintBlocker> PaintBlockers = new List<PaintBlocker>();

    [MenuItem("Tools/Splat Fighters/Build Hangar Arena Map V1")]
    public static void BuildIntoMvpScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Built hangar arena map v1 into the MVP shooting test scene.");
    }

    [MenuItem("Tools/Splat Fighters/Repair Arena Containment Walls")]
    public static void RepairArenaContainmentWalls()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureArenaContainmentCollidersInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Repaired arena containment walls in the MVP shooting test scene.");
    }

    public static void EnsureArenaContainmentCollidersInCurrentScene()
    {
        GameObject levelRoot = GameObject.Find(LevelRootName);

        if (levelRoot == null)
        {
            return;
        }

        Transform boundaryRoot = levelRoot.transform.Find("BoundaryWalls");

        if (boundaryRoot == null)
        {
            boundaryRoot = CreateGroup(levelRoot.transform, "BoundaryWalls");
        }

        RemoveExistingArenaContainmentColliders(boundaryRoot);
        BuildArenaContainmentColliders(boundaryRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public static void BuildInCurrentScene()
    {
        EnsureMaterialFolders();
        EnsureHangarMaterialsUseUrpShaders();
        PaintBlockers.Clear();

        Material boundaryMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Boundary.mat", new Color(0.18f, 0.2f, 0.22f));
        Material coverMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Cover.mat", new Color(0.42f, 0.47f, 0.48f));
        Material platformMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Platform.mat", new Color(0.3f, 0.36f, 0.34f));
        Material rampMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Ramp.mat", new Color(0.45f, 0.42f, 0.36f));
        Material paintRouteMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_PaintRoute.mat", TeamVisualPalette.TeamAColor);
        Material objectiveMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_SplatZone.mat", new Color(1f, 1f, 1f, 0.32f));
        Material towerMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_TowerObjective.mat", new Color(0.88f, 0.88f, 0.92f));
        Material teamAMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Spawn_TeamA.mat", TeamVisualPalette.TeamAColor);
        Material teamBMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Spawn_TeamB.mat", TeamVisualPalette.TeamBColor);
        Material teamAPlayerMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamA_Player.mat", TeamVisualPalette.TeamAColor);
        Material teamBBotMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamB_Bot.mat", TeamVisualPalette.TeamBColor);

        DestroyExistingLevelRoot();

        GameObject levelRoot = new GameObject(LevelRootName);
        Transform boundaryRoot = CreateGroup(levelRoot.transform, "BoundaryWalls");
        Transform obstacleRoot = CreateGroup(levelRoot.transform, "Obstacles");
        Transform coverRoot = CreateGroup(levelRoot.transform, "Cover");
        Transform platformRoot = CreateGroup(levelRoot.transform, "Platforms");
        Transform routeRoot = CreateGroup(levelRoot.transform, "PaintRoutes");
        Transform objectiveRoot = CreateGroup(levelRoot.transform, "Objectives");
        Transform spawnRoot = CreateGroup(levelRoot.transform, "SpawnPoints");
        Transform aiRoot = CreateGroup(levelRoot.transform, "AI");
        Transform hangarVisualRoot = CreateGroup(levelRoot.transform, "HangarAssetVisuals");

        BuildBoundaryWalls(boundaryRoot, boundaryMaterial);
        BuildContestObstacles(obstacleRoot, coverRoot, coverMaterial);
        BuildSidePlatforms(platformRoot, platformMaterial, rampMaterial);
        BuildPaintRoutes(routeRoot, platformMaterial, paintRouteMaterial);
        BuildHangarAssetVisuals(hangarVisualRoot);
        BuildSplatZoneObjective(objectiveRoot, objectiveMaterial);
        BuildTowerObjective(objectiveRoot, towerMaterial);
        BuildSpawnPoints(spawnRoot, teamAMaterial, teamBMaterial);
        BuildTeamBBot(aiRoot, teamBBotMaterial);
        ConfigurePaintableGroundForGrayboxMap();
        PositionExistingPlayerAtTeamASpawn(teamAPlayerMaterial);
        PositionExistingCameraForGrayboxMap();
        ConfigureGameManagerForMatchFlow();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void BuildBoundaryWalls(Transform parent, Material material)
    {
        BuildArenaContainmentColliders(parent);

        CreateSolidCube("NorthBoundaryRail", new Vector3(0f, 0.55f, HalfMapLength + 0.25f), new Vector3(MapWidth + 0.8f, 1.1f, 0.5f), material, parent);
        CreateSolidCube("SouthBoundaryRail", new Vector3(0f, 0.55f, -HalfMapLength - 0.25f), new Vector3(MapWidth + 0.8f, 1.1f, 0.5f), material, parent);
        CreateSolidCube("EastBoundaryRail", new Vector3(HalfMapWidth + 0.25f, 0.55f, 0f), new Vector3(0.5f, 1.1f, MapLength + 0.8f), material, parent);
        CreateSolidCube("WestBoundaryRail", new Vector3(-HalfMapWidth - 0.25f, 0.55f, 0f), new Vector3(0.5f, 1.1f, MapLength + 0.8f), material, parent);

        CreateSolidCube("NorthEastCornerPost", new Vector3(HalfMapWidth + 0.25f, 0.8f, HalfMapLength + 0.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("NorthWestCornerPost", new Vector3(-HalfMapWidth - 0.25f, 0.8f, HalfMapLength + 0.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("SouthEastCornerPost", new Vector3(HalfMapWidth + 0.25f, 0.8f, -HalfMapLength - 0.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("SouthWestCornerPost", new Vector3(-HalfMapWidth - 0.25f, 0.8f, -HalfMapLength - 0.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);

        // Keep spawn exits open. Arena containment colliders already prevent map escape.
    }

    private static void BuildArenaContainmentColliders(Transform parent)
    {
        float verticalCenter = ArenaContainmentHeight * 0.5f;
        float northSouthOffset = HalfMapLength + ArenaContainmentThickness * 0.5f;
        float eastWestOffset = HalfMapWidth + ArenaContainmentThickness * 0.5f;

        CreateContainmentCollider(
            "NorthArenaContainmentWall",
            new Vector3(0f, verticalCenter, northSouthOffset),
            new Vector3(MapWidth + ArenaContainmentThickness * 2f, ArenaContainmentHeight, ArenaContainmentThickness),
            parent);
        CreateContainmentCollider(
            "SouthArenaContainmentWall",
            new Vector3(0f, verticalCenter, -northSouthOffset),
            new Vector3(MapWidth + ArenaContainmentThickness * 2f, ArenaContainmentHeight, ArenaContainmentThickness),
            parent);
        CreateContainmentCollider(
            "EastArenaContainmentWall",
            new Vector3(eastWestOffset, verticalCenter, 0f),
            new Vector3(ArenaContainmentThickness, ArenaContainmentHeight, MapLength + ArenaContainmentThickness * 2f),
            parent);
        CreateContainmentCollider(
            "WestArenaContainmentWall",
            new Vector3(-eastWestOffset, verticalCenter, 0f),
            new Vector3(ArenaContainmentThickness, ArenaContainmentHeight, MapLength + ArenaContainmentThickness * 2f),
            parent);
    }

    private static void RemoveExistingArenaContainmentColliders(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child != null && child.name.EndsWith("ArenaContainmentWall"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void BuildContestObstacles(Transform obstacleRoot, Transform coverRoot, Material material)
    {
        CreateSolidCube("CenterContestBlock", new Vector3(0f, 0.55f, 0f), new Vector3(4.2f, 1.1f, 1.1f), material, obstacleRoot);
        CreateSolidCube("CenterLeftPillar", new Vector3(-3.2f, 0.85f, -0.55f), new Vector3(0.95f, 1.7f, 0.95f), material, obstacleRoot);
        CreateSolidCube("CenterRightPillar", new Vector3(3.2f, 0.85f, 0.55f), new Vector3(0.95f, 1.7f, 0.95f), material, obstacleRoot);
        CreateSolidCube("NorthCenterScreen", new Vector3(0f, 0.5f, 4.45f), new Vector3(5.5f, 1f, 0.8f), material, obstacleRoot);
        CreateSolidCube("SouthCenterScreen", new Vector3(0f, 0.5f, -4.45f), new Vector3(5.5f, 1f, 0.8f), material, obstacleRoot);

        CreateSolidCube("LeftMidCover", new Vector3(-5.2f, 0.45f, -3.4f), new Vector3(1.25f, 0.9f, 3.4f), material, coverRoot);
        CreateSolidCube("RightMidCover", new Vector3(5.2f, 0.45f, 3.4f), new Vector3(1.25f, 0.9f, 3.4f), material, coverRoot);
        CreateSolidCube("LeftCenterCutCover", new Vector3(-6.35f, 0.45f, 1.25f), new Vector3(2.2f, 0.9f, 0.8f), material, coverRoot);
        CreateSolidCube("RightCenterCutCover", new Vector3(6.35f, 0.45f, -1.25f), new Vector3(2.2f, 0.9f, 0.8f), material, coverRoot);

        CreateSolidCube("TeamAForwardCover", new Vector3(2.9f, 0.4f, -8.2f), new Vector3(2.8f, 0.8f, 0.85f), material, coverRoot);
        CreateSolidCube("TeamBForwardCover", new Vector3(-2.9f, 0.4f, 8.2f), new Vector3(2.8f, 0.8f, 0.85f), material, coverRoot);
        CreateSolidCube("TeamAMidLeftCover", new Vector3(-6.4f, 0.4f, -9.8f), new Vector3(2.2f, 0.8f, 1.1f), material, coverRoot);
        CreateSolidCube("TeamBMidRightCover", new Vector3(6.4f, 0.4f, 9.8f), new Vector3(2.2f, 0.8f, 1.1f), material, coverRoot);

        CreateSolidCube("TeamASpawnLeftCover", new Vector3(-4.7f, 0.45f, -14.2f), new Vector3(2.0f, 0.9f, 0.85f), material, coverRoot);
        CreateSolidCube("TeamASpawnRightCover", new Vector3(4.7f, 0.45f, -14.2f), new Vector3(2.0f, 0.9f, 0.85f), material, coverRoot);
        CreateSolidCube("TeamBSpawnLeftCover", new Vector3(-4.7f, 0.45f, 14.2f), new Vector3(2.0f, 0.9f, 0.85f), material, coverRoot);
        CreateSolidCube("TeamBSpawnRightCover", new Vector3(4.7f, 0.45f, 14.2f), new Vector3(2.0f, 0.9f, 0.85f), material, coverRoot);

        CreateSolidCube("LeftLaneLowBlock", new Vector3(-11.3f, 0.35f, -7.2f), new Vector3(2.5f, 0.7f, 1.05f), material, coverRoot);
        CreateSolidCube("RightLaneLowBlock", new Vector3(11.3f, 0.35f, 7.2f), new Vector3(2.5f, 0.7f, 1.05f), material, coverRoot);
        CreateSolidCube("LeftLaneBackCover", new Vector3(-12.1f, 0.45f, 5.6f), new Vector3(1.0f, 0.9f, 3.1f), material, coverRoot);
        CreateSolidCube("RightLaneBackCover", new Vector3(12.1f, 0.45f, -5.6f), new Vector3(1.0f, 0.9f, 3.1f), material, coverRoot);
    }

    private static void BuildSidePlatforms(Transform parent, Material platformMaterial, Material rampMaterial)
    {
        CreateSolidCube("WestSidePlatform", new Vector3(-10.8f, 0.25f, -1.25f), new Vector3(3.2f, 0.5f, 7.2f), platformMaterial, parent);
        CreateSolidCube("EastSidePlatform", new Vector3(10.8f, 0.25f, 1.25f), new Vector3(3.2f, 0.5f, 7.2f), platformMaterial, parent);
        CreateSolidCube("WestFlankPlatform", new Vector3(-13.2f, 0.18f, 8.7f), new Vector3(2.5f, 0.36f, 5.4f), platformMaterial, parent);
        CreateSolidCube("EastFlankPlatform", new Vector3(13.2f, 0.18f, -8.7f), new Vector3(2.5f, 0.36f, 5.4f), platformMaterial, parent);
        CreateSolidCube("NorthPerchPlatform", new Vector3(4.8f, 0.22f, 12.1f), new Vector3(4.8f, 0.44f, 2.2f), platformMaterial, parent);
        CreateSolidCube("SouthPerchPlatform", new Vector3(-4.8f, 0.22f, -12.1f), new Vector3(4.8f, 0.44f, 2.2f), platformMaterial, parent);

        CreateSolidCube("WestPlatformCover", new Vector3(-10.8f, 0.85f, 1.6f), new Vector3(1.8f, 0.7f, 0.8f), platformMaterial, parent);
        CreateSolidCube("EastPlatformCover", new Vector3(10.8f, 0.85f, -1.6f), new Vector3(1.8f, 0.7f, 0.8f), platformMaterial, parent);
        CreateSolidCube("NorthPerchCover", new Vector3(4.8f, 0.8f, 12.7f), new Vector3(2.2f, 0.72f, 0.7f), platformMaterial, parent);
        CreateSolidCube("SouthPerchCover", new Vector3(-4.8f, 0.8f, -12.7f), new Vector3(2.2f, 0.72f, 0.7f), platformMaterial, parent);

        CreateRamp("WestPlatformRamp", new Vector3(-8.35f, 0.18f, -1.4f), new Vector3(2.6f, 0.24f, 4.4f), new Vector3(0f, 0f, -8f), rampMaterial, parent);
        CreateRamp("EastPlatformRamp", new Vector3(8.35f, 0.18f, 1.4f), new Vector3(2.6f, 0.24f, 4.4f), new Vector3(0f, 0f, 8f), rampMaterial, parent);
        CreateRamp("NorthPerchRamp", new Vector3(2.6f, 0.16f, 10.4f), new Vector3(3.2f, 0.22f, 2.5f), new Vector3(7f, 0f, 0f), rampMaterial, parent);
        CreateRamp("SouthPerchRamp", new Vector3(-2.6f, 0.16f, -10.4f), new Vector3(3.2f, 0.22f, 2.5f), new Vector3(-7f, 0f, 0f), rampMaterial, parent);
    }

    private static void BuildPaintRoutes(Transform parent, Material platformMaterial, Material routeMaterial)
    {
        CreateSolidCube("WestPaintRouteUpperDeck", new Vector3(-11.35f, 1.55f, -2.2f), new Vector3(2.6f, 0.3f, 2.1f), platformMaterial, parent);

        Transform routeProbe = CreateMarker("WestPaintRouteProbe", new Vector3(-9.55f, 0f, -2.2f), parent);
        GameObject routeSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routeSurface.name = "WestPaintRouteSurface";
        routeSurface.transform.SetParent(parent, false);
        routeSurface.transform.position = new Vector3(-10.05f, 1.05f, -2.2f);
        routeSurface.transform.localScale = new Vector3(0.35f, 2.2f, 1.45f);
        AssignMaterial(routeSurface, routeMaterial);

        Collider routeCollider = routeSurface.GetComponent<Collider>();

        if (routeCollider != null)
        {
            routeCollider.isTrigger = true;
        }

        PaintRouteSurface route = routeSurface.AddComponent<PaintRouteSurface>();
        route.Configure(Team.TeamA, routeProbe, Vector3.up, 4.2f);
        EditorUtility.SetDirty(routeSurface);
        EditorUtility.SetDirty(route);

        CreateSolidCube("EastPaintRouteUpperDeck", new Vector3(11.35f, 1.55f, 2.2f), new Vector3(2.6f, 0.3f, 2.1f), platformMaterial, parent);

        Transform eastProbe = CreateMarker("EastPaintRouteProbe", new Vector3(9.55f, 0f, 2.2f), parent);
        GameObject eastRouteSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eastRouteSurface.name = "EastPaintRouteSurface";
        eastRouteSurface.transform.SetParent(parent, false);
        eastRouteSurface.transform.position = new Vector3(10.05f, 1.05f, 2.2f);
        eastRouteSurface.transform.localScale = new Vector3(0.35f, 2.2f, 1.45f);
        AssignMaterial(eastRouteSurface, routeMaterial);

        Collider eastCollider = eastRouteSurface.GetComponent<Collider>();

        if (eastCollider != null)
        {
            eastCollider.isTrigger = true;
        }

        PaintRouteSurface eastRoute = eastRouteSurface.AddComponent<PaintRouteSurface>();
        eastRoute.Configure(Team.TeamA, eastProbe, Vector3.up, 4.2f);
        EditorUtility.SetDirty(eastRouteSurface);
        EditorUtility.SetDirty(eastRoute);
    }

    private static void BuildSplatZoneObjective(Transform parent, Material material)
    {
        GameObject zoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zoneObject.name = "CenterSplatZone";
        zoneObject.transform.SetParent(parent, false);
        zoneObject.transform.position = new Vector3(0f, 0.035f, 0f);
        zoneObject.transform.localScale = new Vector3(7.5f, 0.05f, 4.2f);
        AssignMaterial(zoneObject, material);

        Collider collider = zoneObject.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        SplatZoneObjective zone = zoneObject.AddComponent<SplatZoneObjective>();
        SerializedObject zoneSo = new SerializedObject(zone);
        zoneSo.FindProperty("zoneSize").vector2Value = new Vector2(7.5f, 4.2f);
        zoneSo.FindProperty("zoneHeight").floatValue = 0.5f;
        zoneSo.FindProperty("controlThresholdPercent").floatValue = 55f;
        zoneSo.FindProperty("minimumPaintedPercent").floatValue = 18f;
        zoneSo.FindProperty("refreshInterval").floatValue = 0.25f;
        zoneSo.FindProperty("zoneRenderer").objectReferenceValue = zoneObject.GetComponent<MeshRenderer>();
        zoneSo.FindProperty("neutralColor").colorValue = new Color(1f, 1f, 1f, 0.28f);
        zoneSo.FindProperty("contestedColor").colorValue = new Color(1f, 0.95f, 0.2f, 0.38f);
        zoneSo.FindProperty("teamAColor").colorValue = TeamVisualPalette.TeamAOverlayColor;
        zoneSo.FindProperty("teamBColor").colorValue = TeamVisualPalette.TeamBOverlayColor;
        zoneSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(zoneObject);
        EditorUtility.SetDirty(zone);
    }

    private static void BuildTowerObjective(Transform parent, Material material)
    {
        Transform routeRoot = CreateGroup(parent, "CenterTowerRoute");
        Transform teamBGoal = CreateMarker("CenterTower_TeamBGoal", new Vector3(0f, 0f, -10.8f), routeRoot);
        Transform centerPoint = CreateMarker("CenterTower_CenterPoint", new Vector3(0f, 0f, 0f), routeRoot);
        Transform teamAGoal = CreateMarker("CenterTower_TeamAGoal", new Vector3(0f, 0f, 10.8f), routeRoot);

        GameObject towerRoot = new GameObject("CenterTowerObjective");
        towerRoot.transform.SetParent(parent, false);
        towerRoot.transform.position = centerPoint.position;

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "CenterTowerPlatform";
        platform.transform.SetParent(towerRoot.transform, false);
        platform.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        platform.transform.localScale = new Vector3(1.6f, 0.18f, 1.6f);
        AssignMaterial(platform, material);

        Collider platformCollider = platform.GetComponent<Collider>();

        if (platformCollider != null)
        {
            Object.DestroyImmediate(platformCollider);
        }

        GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.name = "CenterTowerMast";
        mast.transform.SetParent(towerRoot.transform, false);
        mast.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        mast.transform.localScale = new Vector3(0.18f, 0.72f, 0.18f);
        AssignMaterial(mast, material);

        Collider mastCollider = mast.GetComponent<Collider>();

        if (mastCollider != null)
        {
            Object.DestroyImmediate(mastCollider);
        }

        TowerObjective tower = towerRoot.AddComponent<TowerObjective>();
        SerializedObject towerSo = new SerializedObject(tower);
        towerSo.FindProperty("teamBGoal").objectReferenceValue = teamBGoal;
        towerSo.FindProperty("centerPoint").objectReferenceValue = centerPoint;
        towerSo.FindProperty("teamAGoal").objectReferenceValue = teamAGoal;
        towerSo.FindProperty("routeProgress").floatValue = 0f;
        towerSo.FindProperty("moveSpeed").floatValue = 0.18f;
        towerSo.FindProperty("controlSize").vector2Value = new Vector2(4.4f, 3.2f);
        towerSo.FindProperty("controlHeight").floatValue = 0.5f;
        towerSo.FindProperty("controlThresholdPercent").floatValue = 52f;
        towerSo.FindProperty("minimumPaintedPercent").floatValue = 18f;
        towerSo.FindProperty("refreshInterval").floatValue = 0.25f;
        towerSo.FindProperty("towerRenderer").objectReferenceValue = platform.GetComponent<MeshRenderer>();
        towerSo.FindProperty("neutralColor").colorValue = Color.white;
        towerSo.FindProperty("contestedColor").colorValue = new Color(1f, 0.95f, 0.2f, 1f);
        towerSo.FindProperty("teamAColor").colorValue = TeamVisualPalette.TeamAColor;
        towerSo.FindProperty("teamBColor").colorValue = TeamVisualPalette.TeamBColor;
        towerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(towerRoot);
        EditorUtility.SetDirty(platform);
        EditorUtility.SetDirty(mast);
        EditorUtility.SetDirty(tower);
    }

    private static void BuildHangarAssetVisuals(Transform parent)
    {
        if (!HasHangarAssets())
        {
            Debug.LogWarning("Hangar Building Modular assets were not found. The MVP scene will keep gameplay blockers only.");
            return;
        }

        BuildHangarLargeSurfaces(parent);
        BuildHangarFloor(parent);
        BuildHangarShell(parent);
        BuildHangarGameplayProps(parent);
        BuildHangarPlatformVisuals(parent);
    }

    private static bool HasHangarAssets()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabRoot + "pref_floor.prefab") != null
            && AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabRoot + "pref_wall.prefab") != null;
    }

    private static void EnsureHangarMaterialsUseUrpShaders()
    {
        const string materialFolder = "Assets/Hangar Building Modular/Materials";

        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            return;
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (litShader == null)
        {
            Debug.LogWarning("URP Lit shader was not found. Hangar materials may render pink until the URP package is available.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialFolder });

        for (int i = 0; i < materialGuids.Length; i++)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                continue;
            }

            bool useUnlitWallShader = ShouldUseUnlitHangarWallMaterial(materialPath) && unlitShader != null;
            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            material.shader = useUnlitWallShader ? unlitShader : litShader;

            if (mainTexture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (useUnlitWallShader && material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.35f);
            }

            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
    }

    private static bool ShouldUseUnlitHangarWallMaterial(string materialPath)
    {
        string fileName = System.IO.Path.GetFileName(materialPath);
        return fileName == "mat_wall.mat"
            || fileName == "mat_wall_frame.mat"
            || fileName == "mat_frame.mat";
    }

    private static void BuildHangarFloor(Transform parent)
    {
        CreateHangarVisual("HangarMainFloor", "pref_floor.prefab", new Vector3(0f, -0.075f, 0f), Vector3.zero, new Vector3(MapWidth, 0.12f, MapLength), parent);
    }

    private static void BuildHangarLargeSurfaces(Transform parent)
    {
        Material floorMaterial = LoadHangarMaterial("mat_floor.mat");
        Material wallMaterial = LoadHangarMaterial("mat_wall.mat");

        CreateTexturedSurface("HangarPaintableFloorSurface", new Vector3(0f, -0.052f, 0f), new Vector3(MapWidth, 0.025f, MapLength), floorMaterial, parent);
        CreateTexturedSurface("HangarNorthWallSurface", new Vector3(0f, 3.1f, HalfMapLength + 0.42f), new Vector3(MapWidth + 1.2f, 6.2f, 0.12f), wallMaterial, parent);
        CreateTexturedSurface("HangarSouthWallSurface", new Vector3(0f, 3.1f, -HalfMapLength - 0.42f), new Vector3(MapWidth + 1.2f, 6.2f, 0.12f), wallMaterial, parent);
        CreateTexturedSurface("HangarEastWallSurface", new Vector3(HalfMapWidth + 0.42f, 3.1f, 0f), new Vector3(0.12f, 6.2f, MapLength + 1.2f), wallMaterial, parent);
        CreateTexturedSurface("HangarWestWallSurface", new Vector3(-HalfMapWidth - 0.42f, 3.1f, 0f), new Vector3(0.12f, 6.2f, MapLength + 1.2f), wallMaterial, parent);
    }

    private static void BuildHangarShell(Transform parent)
    {
        float[] frameXPositions = { -HalfMapWidth + 2.2f, -5.4f, 5.4f, HalfMapWidth - 2.2f };

        for (int i = 0; i < frameXPositions.Length; i++)
        {
            float x = frameXPositions[i];
            CreateHangarVisual($"HangarNorthFrame_{i + 1}", "pref_frame.prefab", new Vector3(x, 3.2f, HalfMapLength - 0.35f), Vector3.zero, new Vector3(1.1f, 5.4f, 1.0f), parent);
            CreateHangarVisual($"HangarSouthFrame_{i + 1}", "pref_frame.prefab", new Vector3(x, 3.2f, -HalfMapLength + 0.35f), new Vector3(0f, 180f, 0f), new Vector3(1.1f, 5.4f, 1.0f), parent);
        }
    }

    private static void BuildHangarGameplayProps(Transform parent)
    {
        CreateHangarVisual("HangarCenterGenerator", "pref_generator.prefab", new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(4.2f, 1.15f, 1.45f), parent);
        CreateHangarVisual("HangarCenterLeftScaffold", "pref_scaffold_01.prefab", new Vector3(-3.2f, 0.85f, -0.55f), new Vector3(0f, 20f, 0f), new Vector3(1.15f, 1.7f, 1.15f), parent);
        CreateHangarVisual("HangarCenterRightScaffold", "pref_scaffold_01.prefab", new Vector3(3.2f, 0.85f, 0.55f), new Vector3(0f, -160f, 0f), new Vector3(1.15f, 1.7f, 1.15f), parent);
        CreateHangarVisual("HangarNorthCenterBarrier", "pref_pallet.prefab", new Vector3(0f, 0.5f, 4.45f), new Vector3(0f, 90f, 0f), new Vector3(5.5f, 1f, 0.85f), parent);
        CreateHangarVisual("HangarSouthCenterBarrier", "pref_pallet.prefab", new Vector3(0f, 0.5f, -4.45f), new Vector3(0f, 90f, 0f), new Vector3(5.5f, 1f, 0.85f), parent);

        CreateHangarVisual("HangarLeftMidCrates", "pref_box_01.prefab", new Vector3(-5.2f, 0.45f, -3.4f), new Vector3(0f, 8f, 0f), new Vector3(1.3f, 0.9f, 3.4f), parent);
        CreateHangarVisual("HangarRightMidCrates", "pref_box_01.prefab", new Vector3(5.2f, 0.45f, 3.4f), new Vector3(0f, 188f, 0f), new Vector3(1.3f, 0.9f, 3.4f), parent);
        CreateHangarVisual("HangarLeftCenterToolboxes", "pref_toolbox.prefab", new Vector3(-6.35f, 0.45f, 1.25f), Vector3.zero, new Vector3(2.2f, 0.9f, 0.9f), parent);
        CreateHangarVisual("HangarRightCenterToolboxes", "pref_toolbox.prefab", new Vector3(6.35f, 0.45f, -1.25f), new Vector3(0f, 180f, 0f), new Vector3(2.2f, 0.9f, 0.9f), parent);

        CreateHangarVisual("HangarTeamAForwardCargo", "pref_box_02.prefab", new Vector3(2.9f, 0.4f, -8.2f), new Vector3(0f, 90f, 0f), new Vector3(2.8f, 0.8f, 0.9f), parent);
        CreateHangarVisual("HangarTeamBForwardCargo", "pref_box_02.prefab", new Vector3(-2.9f, 0.4f, 8.2f), new Vector3(0f, -90f, 0f), new Vector3(2.8f, 0.8f, 0.9f), parent);
        CreateHangarVisual("HangarTeamAMidPallets", "pref_pallet.prefab", new Vector3(-6.4f, 0.4f, -9.8f), new Vector3(0f, 90f, 0f), new Vector3(2.2f, 0.8f, 1.1f), parent);
        CreateHangarVisual("HangarTeamBMidPallets", "pref_pallet.prefab", new Vector3(6.4f, 0.4f, 9.8f), new Vector3(0f, 90f, 0f), new Vector3(2.2f, 0.8f, 1.1f), parent);

        CreateHangarVisual("HangarTeamASpawnLeftCargo", "pref_box_01.prefab", new Vector3(-4.7f, 0.45f, -14.2f), Vector3.zero, new Vector3(2f, 0.9f, 0.9f), parent);
        CreateHangarVisual("HangarTeamASpawnRightCargo", "pref_box_01.prefab", new Vector3(4.7f, 0.45f, -14.2f), Vector3.zero, new Vector3(2f, 0.9f, 0.9f), parent);
        CreateHangarVisual("HangarTeamBSpawnLeftCargo", "pref_box_01.prefab", new Vector3(-4.7f, 0.45f, 14.2f), Vector3.zero, new Vector3(2f, 0.9f, 0.9f), parent);
        CreateHangarVisual("HangarTeamBSpawnRightCargo", "pref_box_01.prefab", new Vector3(4.7f, 0.45f, 14.2f), Vector3.zero, new Vector3(2f, 0.9f, 0.9f), parent);

        CreateHangarVisual("HangarLeftLaneTrolley", "pref_trolley.prefab", new Vector3(-11.3f, 0.35f, -7.2f), new Vector3(0f, 90f, 0f), new Vector3(2.5f, 0.75f, 1.05f), parent);
        CreateHangarVisual("HangarRightLaneTrolley", "pref_trolley.prefab", new Vector3(11.3f, 0.35f, 7.2f), new Vector3(0f, -90f, 0f), new Vector3(2.5f, 0.75f, 1.05f), parent);
        CreateHangarVisual("HangarLeftLaneScaffold", "pref_scaffold_02.prefab", new Vector3(-12.1f, 0.6f, 5.6f), Vector3.zero, new Vector3(1f, 1.2f, 3.1f), parent);
        CreateHangarVisual("HangarRightLaneScaffold", "pref_scaffold_02.prefab", new Vector3(12.1f, 0.6f, -5.6f), Vector3.zero, new Vector3(1f, 1.2f, 3.1f), parent);
    }

    private static void BuildHangarPlatformVisuals(Transform parent)
    {
        CreateHangarVisual("HangarWestSideDeck", "pref_scaffold_02.prefab", new Vector3(-10.8f, 0.25f, -1.25f), Vector3.zero, new Vector3(3.2f, 0.55f, 7.2f), parent);
        CreateHangarVisual("HangarEastSideDeck", "pref_scaffold_02.prefab", new Vector3(10.8f, 0.25f, 1.25f), Vector3.zero, new Vector3(3.2f, 0.55f, 7.2f), parent);
        CreateHangarVisual("HangarWestFlankDeck", "pref_floor.prefab", new Vector3(-13.2f, 0.18f, 8.7f), Vector3.zero, new Vector3(2.5f, 0.22f, 5.4f), parent);
        CreateHangarVisual("HangarEastFlankDeck", "pref_floor.prefab", new Vector3(13.2f, 0.18f, -8.7f), Vector3.zero, new Vector3(2.5f, 0.22f, 5.4f), parent);
        CreateHangarVisual("HangarNorthPerchDeck", "pref_floor.prefab", new Vector3(4.8f, 0.22f, 12.1f), Vector3.zero, new Vector3(4.8f, 0.25f, 2.2f), parent);
        CreateHangarVisual("HangarSouthPerchDeck", "pref_floor.prefab", new Vector3(-4.8f, 0.22f, -12.1f), Vector3.zero, new Vector3(4.8f, 0.25f, 2.2f), parent);

        CreateHangarVisual("HangarWestPlatformCrate", "pref_box_02.prefab", new Vector3(-10.8f, 0.85f, 1.6f), Vector3.zero, new Vector3(1.8f, 0.7f, 0.8f), parent);
        CreateHangarVisual("HangarEastPlatformCrate", "pref_box_02.prefab", new Vector3(10.8f, 0.85f, -1.6f), Vector3.zero, new Vector3(1.8f, 0.7f, 0.8f), parent);
        CreateHangarVisual("HangarNorthPerchCrate", "pref_box_02.prefab", new Vector3(4.8f, 0.8f, 12.7f), Vector3.zero, new Vector3(2.2f, 0.72f, 0.7f), parent);
        CreateHangarVisual("HangarSouthPerchCrate", "pref_box_02.prefab", new Vector3(-4.8f, 0.8f, -12.7f), Vector3.zero, new Vector3(2.2f, 0.72f, 0.7f), parent);

        CreateHangarVisual("HangarWestRampPanel", "pref_floor.prefab", new Vector3(-8.35f, 0.18f, -1.4f), new Vector3(0f, 0f, -8f), new Vector3(2.6f, 0.18f, 4.4f), parent);
        CreateHangarVisual("HangarEastRampPanel", "pref_floor.prefab", new Vector3(8.35f, 0.18f, 1.4f), new Vector3(0f, 0f, 8f), new Vector3(2.6f, 0.18f, 4.4f), parent);
        CreateHangarVisual("HangarNorthRampPanel", "pref_floor.prefab", new Vector3(2.6f, 0.16f, 10.4f), new Vector3(7f, 0f, 0f), new Vector3(3.2f, 0.18f, 2.5f), parent);
        CreateHangarVisual("HangarSouthRampPanel", "pref_floor.prefab", new Vector3(-2.6f, 0.16f, -10.4f), new Vector3(-7f, 0f, 0f), new Vector3(3.2f, 0.18f, 2.5f), parent);
    }

    private static void BuildHangarLightingProps(Transform parent)
    {
        float[] xPositions = { -11.5f, -3.8f, 3.8f, 11.5f };

        for (int i = 0; i < xPositions.Length; i++)
        {
            CreateHangarVisual($"HangarNorthLamp_{i + 1}", "pref_lamp.prefab", new Vector3(xPositions[i], 5.6f, HalfMapLength - 1.05f), new Vector3(0f, 180f, 0f), new Vector3(1.1f, 1.1f, 1.1f), parent);
            CreateHangarVisual($"HangarSouthLamp_{i + 1}", "pref_lamp.prefab", new Vector3(xPositions[i], 5.6f, -HalfMapLength + 1.05f), Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f), parent);
        }
    }

    private static GameObject CreateHangarVisual(string name, string prefabFileName, Vector3 position, Vector3 eulerAngles, Vector3 targetWorldSize, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabRoot + prefabFileName);

        if (prefab == null)
        {
            Debug.LogWarning($"Missing hangar prefab: {prefabFileName}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            return null;
        }

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(eulerAngles);
        instance.transform.localScale = Vector3.one;

        RemoveRuntimeCostFromVisual(instance);
        FitVisualToTargetSize(instance, targetWorldSize, position);
        MarkStaticRecursive(instance);
        EditorUtility.SetDirty(instance);
        return instance;
    }

    private static void RemoveRuntimeCostFromVisual(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Object.DestroyImmediate(rigidbodies[i]);
        }

        Light[] lights = visual.GetComponentsInChildren<Light>(true);

        for (int i = 0; i < lights.Length; i++)
        {
            Object.DestroyImmediate(lights[i]);
        }

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = ShadowCastingMode.Off;
            renderers[i].receiveShadows = false;
            EditorUtility.SetDirty(renderers[i]);
        }
    }

    private static GameObject CreateTexturedSurface(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        if (material == null)
        {
            return null;
        }

        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = name;
        surface.transform.SetParent(parent, false);
        surface.transform.position = position;
        surface.transform.rotation = Quaternion.identity;
        surface.transform.localScale = scale;
        AssignMaterial(surface, material);
        MeshRenderer renderer = surface.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
        }

        Collider collider = surface.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        MarkStaticRecursive(surface);
        EditorUtility.SetDirty(surface);
        return surface;
    }

    private static Material LoadHangarMaterial(string materialFileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(HangarMaterialRoot + materialFileName);
    }

    private static void FitVisualToTargetSize(GameObject visual, Vector3 targetWorldSize, Vector3 targetCenter)
    {
        if (!TryGetRendererBounds(visual, out Bounds bounds))
        {
            return;
        }

        Vector3 currentSize = bounds.size;
        Vector3 scale = visual.transform.localScale;

        if (targetWorldSize.x > 0.001f && currentSize.x > 0.001f)
        {
            scale.x *= targetWorldSize.x / currentSize.x;
        }

        if (targetWorldSize.y > 0.001f && currentSize.y > 0.001f)
        {
            scale.y *= targetWorldSize.y / currentSize.y;
        }

        if (targetWorldSize.z > 0.001f && currentSize.z > 0.001f)
        {
            scale.z *= targetWorldSize.z / currentSize.z;
        }

        visual.transform.localScale = scale;

        if (TryGetRendererBounds(visual, out bounds))
        {
            visual.transform.position += targetCenter - bounds.center;
        }
    }

    private static bool TryGetRendererBounds(GameObject visual, out Bounds bounds)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(visual.transform.position, Vector3.zero);
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private static void MarkStaticRecursive(GameObject root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            GameObjectUtility.SetStaticEditorFlags(transforms[i].gameObject, StaticEditorFlags.BatchingStatic);
            EditorUtility.SetDirty(transforms[i].gameObject);
        }
    }

    private static void BuildSpawnPoints(Transform parent, Material teamAMaterial, Material teamBMaterial)
    {
        CreateSpawnPoint("TeamASpawn", Team.TeamA, new Vector3(0f, CharacterRootHeight, -15.2f), Vector3.forward, teamAMaterial, parent);
        CreateSpawnPoint("TeamBSpawn", Team.TeamB, new Vector3(0f, CharacterRootHeight, 15.2f), Vector3.back, teamBMaterial, parent);
    }

    private static void BuildTeamBBot(Transform parent, Material teamBMaterial)
    {
        InkProjectile projectilePrefab = AssetDatabase.LoadAssetAtPath<InkProjectile>(ProjectilePrefabPath);

        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bot.name = "TeamBBot";
        bot.transform.SetParent(parent, false);
        bot.transform.position = new Vector3(0f, CharacterRootHeight, 14.15f);
        bot.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        CapsuleCollider capsuleCollider = bot.GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            Object.DestroyImmediate(capsuleCollider);
        }

        CharacterController characterController = bot.AddComponent<CharacterController>();
        characterController.height = CharacterControllerHeight;
        characterController.radius = CharacterControllerRadius;
        characterController.center = Vector3.zero;
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.24f;

        AssignMaterial(bot, teamBMaterial);
        TeamVisualBinder visualBinder = bot.AddComponent<TeamVisualBinder>();
        visualBinder.Configure(Team.TeamB, null, teamBMaterial);
        CharacterHealth health = bot.AddComponent<CharacterHealth>();
        ConfigureCharacterHealth(health, Team.TeamB, bot.transform);

        GameObject firePointObject = new GameObject("TeamBBotFirePoint");
        firePointObject.transform.SetParent(bot.transform, false);
        firePointObject.transform.localPosition = new Vector3(0f, 0.35f, 0.7f);
        firePointObject.transform.localRotation = Quaternion.identity;

        Transform patrolRoot = CreateGroup(parent, "TeamBBotPatrolPoints");
        Transform[] waypoints =
        {
            CreateMarker("TeamBBotPatrol_01", new Vector3(0f, 1f, 14.15f), patrolRoot),
            CreateMarker("TeamBBotPatrol_02", new Vector3(-6.2f, 1f, 10.2f), patrolRoot),
            CreateMarker("TeamBBotPatrol_03", new Vector3(-2.3f, 1f, 5.7f), patrolRoot),
            CreateMarker("TeamBBotPatrol_04", new Vector3(0.4f, 1f, 2.4f), patrolRoot),
            CreateMarker("TeamBBotPatrol_05", new Vector3(5.8f, 1f, 6.7f), patrolRoot),
            CreateMarker("TeamBBotPatrol_06", new Vector3(8.8f, 1f, 11.2f), patrolRoot)
        };

        Transform paintTargetRoot = CreateGroup(parent, "TeamBBotPaintTargets");
        Transform[] paintTargets =
        {
            CreateMarker("TeamBBotPaintTarget_Center", new Vector3(0f, 0f, 2.4f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_LeftLane", new Vector3(-8.4f, 0f, -1.2f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_RightLane", new Vector3(8.4f, 0f, -2.2f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_TeamASide", new Vector3(0f, 0f, -8.5f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_TeamAFlank", new Vector3(-10.4f, 0f, -10.2f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_TeamAObjective", new Vector3(3.8f, 0f, -5.4f), paintTargetRoot)
        };

        InkWeapon weapon = bot.AddComponent<InkWeapon>();
        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePointObject.transform;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamB;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = 1.6f;
        weaponSo.FindProperty("fireCooldown").floatValue = 0.35f;
        weaponSo.FindProperty("useInkResource").boolValue = true;
        weaponSo.FindProperty("maxInk").floatValue = 100f;
        weaponSo.FindProperty("inkPerShot").floatValue = 10f;
        weaponSo.FindProperty("inkRecoveryPerSecond").floatValue = 12f;
        weaponSo.FindProperty("ownPaintRecoveryMultiplier").floatValue = 3.5f;
        weaponSo.FindProperty("startWithFullInk").boolValue = true;
        weaponSo.FindProperty("groundProbe").objectReferenceValue = bot.transform;
        weaponSo.FindProperty("useCameraAim").boolValue = false;
        weaponSo.FindProperty("paintDirectlyAtAimTarget").boolValue = true;
        weaponSo.FindProperty("projectileIsVisualOnlyWhenDirectPainting").boolValue = true;
        weaponSo.FindProperty("applyTeamColorToProjectile").boolValue = true;
        weaponSo.FindProperty("teamAProjectileColor").colorValue = TeamVisualPalette.TeamAColor;
        weaponSo.FindProperty("teamBProjectileColor").colorValue = TeamVisualPalette.TeamBColor;
        weaponSo.FindProperty("enableKeyboardTestFire").boolValue = false;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();

        BotController botController = bot.AddComponent<BotController>();
        SerializedObject botSo = new SerializedObject(botController);
        botSo.FindProperty("characterController").objectReferenceValue = characterController;
        botSo.FindProperty("weapon").objectReferenceValue = weapon;
        botSo.FindProperty("health").objectReferenceValue = health;
        botSo.FindProperty("firePoint").objectReferenceValue = firePointObject.transform;
        botSo.FindProperty("botTeam").enumValueIndex = (int)Team.TeamB;
        botSo.FindProperty("priorityPaintTargetTeam").enumValueIndex = (int)Team.TeamA;
        AssignTransformArray(botSo.FindProperty("waypoints"), waypoints);
        GameObject teamBSpawn = GameObject.Find("TeamBSpawn");
        botSo.FindProperty("retreatTarget").objectReferenceValue = teamBSpawn != null ? teamBSpawn.transform : waypoints[0];
        AssignTransformArray(botSo.FindProperty("paintTargets"), paintTargets);
        botSo.FindProperty("moveSpeed").floatValue = 3.2f;
        botSo.FindProperty("turnSpeed").floatValue = 540f;
        botSo.FindProperty("waypointReachDistance").floatValue = 0.6f;
        botSo.FindProperty("useTerritoryAwareAim").boolValue = true;
        botSo.FindProperty("targetUnpaintedCellsAfterEnemyPaint").boolValue = true;
        botSo.FindProperty("territorySearchRadius").floatValue = 22f;
        botSo.FindProperty("fireInterval").floatValue = 0.65f;
        botSo.FindProperty("aimRefreshInterval").floatValue = 1.2f;
        botSo.FindProperty("retreatWhenPressured").boolValue = true;
        botSo.FindProperty("lowInkRetreatPercent").floatValue = 28f;
        botSo.FindProperty("resumeInkPercent").floatValue = 62f;
        botSo.FindProperty("lowHealthRetreatPercent").floatValue = 45f;
        botSo.FindProperty("retreatReachDistance").floatValue = 0.9f;
        botSo.FindProperty("retreatRecoveryMultiplier").floatValue = 1.35f;
        botSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(bot);
        EditorUtility.SetDirty(visualBinder);
    }

    private static void CreateSpawnPoint(string name, Team team, Vector3 position, Vector3 forward, Material material, Transform parent)
    {
        GameObject spawnRoot = new GameObject(name);
        spawnRoot.transform.SetParent(parent, false);
        spawnRoot.transform.position = position;
        spawnRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        SpawnPoint spawnPoint = spawnRoot.AddComponent<SpawnPoint>();
        spawnPoint.Configure(team, true);

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = $"{name}Pad";
        pad.transform.SetParent(spawnRoot.transform, false);
        pad.transform.localPosition = new Vector3(0f, -CharacterRootHeight + 0.025f, 0f);
        pad.transform.localRotation = Quaternion.identity;
        pad.transform.localScale = new Vector3(2.4f, 0.05f, 2.4f);

        Collider padCollider = pad.GetComponent<Collider>();

        if (padCollider != null)
        {
            Object.DestroyImmediate(padCollider);
        }

        AssignMaterial(pad, material);
    }

    private static void PositionExistingPlayerAtTeamASpawn(Material teamAMaterial)
    {
        GameObject player = GameObject.Find("Player");
        GameObject spawn = GameObject.Find("TeamASpawn");

        if (player == null || spawn == null)
        {
            return;
        }

        player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        ConfigureCharacterController(player);
        AssignMaterial(player, teamAMaterial);
        TeamVisualBinder visualBinder = player.GetComponent<TeamVisualBinder>();

        if (visualBinder == null)
        {
            visualBinder = player.AddComponent<TeamVisualBinder>();
        }

        ConfigurePlayerInkResource(player);
        ConfigurePlayerSwimForm(player, teamAMaterial);
        ConfigureCharacterHealth(GetOrCreateCharacterHealth(player), Team.TeamA, player.transform);
        ConfigurePlayerSpecialMeter(player);
        ConfigurePlayerSpecialPaintBurst(player);
        ConfigurePlayerRollerTool(player, teamAMaterial);
        ConfigurePlayerToolSwitcher(player);
        visualBinder.Configure(Team.TeamA, teamAMaterial, null);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(visualBinder);
    }

    private static void ConfigurePlayerSpecialMeter(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        SpecialMeter specialMeter = player.GetComponent<SpecialMeter>();

        if (specialMeter == null)
        {
            specialMeter = player.AddComponent<SpecialMeter>();
        }

        SerializedObject specialSo = new SerializedObject(specialMeter);
        specialSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        specialSo.FindProperty("changedCellsForFullCharge").intValue = 180;
        specialSo.FindProperty("startingChargePercent").floatValue = 0f;
        specialSo.FindProperty("resetWhenPaintCleared").boolValue = true;
        specialSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(specialMeter);
    }

    private static void ConfigurePlayerSpecialPaintBurst(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        SpecialMeter specialMeter = player.GetComponent<SpecialMeter>();
        AimController aimController = player.GetComponent<AimController>();
        SpecialPaintBurst specialPaintBurst = player.GetComponent<SpecialPaintBurst>();

        if (specialPaintBurst == null)
        {
            specialPaintBurst = player.AddComponent<SpecialPaintBurst>();
        }

        SerializedObject burstSo = new SerializedObject(specialPaintBurst);
        burstSo.FindProperty("specialMeter").objectReferenceValue = specialMeter;
        burstSo.FindProperty("aimController").objectReferenceValue = aimController;
        burstSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        burstSo.FindProperty("burstPaintRadius").floatValue = 4.25f;
        burstSo.FindProperty("fallbackDistance").floatValue = 4.5f;
        burstSo.FindProperty("activationKey").intValue = (int)KeyCode.Q;
        burstSo.FindProperty("requireMatchPlaying").boolValue = true;
        burstSo.FindProperty("spawnInkSplatterVfx").boolValue = true;
        burstSo.FindProperty("splatterRadiusMultiplier").floatValue = 1.15f;
        burstSo.FindProperty("logActivation").boolValue = false;
        burstSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(specialPaintBurst);
    }

    private static void ConfigurePlayerRollerTool(GameObject player, Material teamAMaterial)
    {
        if (player == null)
        {
            return;
        }

        GameObject rollerTool = GetOrCreateRollerTool(player.transform, teamAMaterial);
        RollerPaintTool rollerPaintTool = rollerTool.GetComponent<RollerPaintTool>();

        if (rollerPaintTool == null)
        {
            rollerPaintTool = rollerTool.AddComponent<RollerPaintTool>();
        }

        SerializedObject rollerSo = new SerializedObject(rollerPaintTool);
        rollerSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        rollerSo.FindProperty("paintKey").intValue = (int)KeyCode.Mouse0;
        rollerSo.FindProperty("requireInput").boolValue = true;
        rollerSo.FindProperty("requireMatchPlaying").boolValue = true;
        rollerSo.FindProperty("paintOrigin").objectReferenceValue = player.transform;
        rollerSo.FindProperty("paintInterval").floatValue = 0.08f;
        rollerSo.FindProperty("paintRadius").floatValue = 1.05f;
        rollerSo.FindProperty("forwardOffset").floatValue = 1.15f;
        rollerSo.FindProperty("halfWidth").floatValue = 0.65f;
        rollerSo.FindProperty("swathSamples").intValue = 3;
        rollerSo.FindProperty("fallbackPaintPlaneY").floatValue = 0f;
        rollerSo.FindProperty("groundProbeLayers").intValue = ~0;
        rollerSo.FindProperty("requireMovementForTrail").boolValue = true;
        rollerSo.FindProperty("minMoveDistance").floatValue = 0.06f;
        rollerSo.ApplyModifiedPropertiesWithoutUndo();
        rollerPaintTool.enabled = false;

        EditorUtility.SetDirty(rollerTool);
        EditorUtility.SetDirty(rollerPaintTool);
    }

    private static void ConfigurePlayerToolSwitcher(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        InkWeapon weapon = player.GetComponentInChildren<InkWeapon>();
        RollerPaintTool rollerPaintTool = player.GetComponentInChildren<RollerPaintTool>(true);
        PlayerToolSwitcher toolSwitcher = player.GetComponent<PlayerToolSwitcher>();

        if (toolSwitcher == null)
        {
            toolSwitcher = player.AddComponent<PlayerToolSwitcher>();
        }

        SerializedObject toolSo = new SerializedObject(toolSwitcher);
        toolSo.FindProperty("defaultTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("currentTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("shooter").objectReferenceValue = weapon;
        toolSo.FindProperty("roller").objectReferenceValue = rollerPaintTool;
        toolSo.FindProperty("enableKeyboardSwitching").boolValue = true;
        toolSo.FindProperty("shooterKey").intValue = (int)KeyCode.Alpha1;
        toolSo.FindProperty("rollerKey").intValue = (int)KeyCode.Alpha2;
        SerializedProperty rollerRenderersProperty = toolSo.FindProperty("rollerRenderers");
        rollerRenderersProperty.ClearArray();

        if (rollerPaintTool != null)
        {
            Renderer[] rollerRenderers = rollerPaintTool.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < rollerRenderers.Length; i++)
            {
                rollerRenderersProperty.InsertArrayElementAtIndex(i);
                rollerRenderersProperty.GetArrayElementAtIndex(i).objectReferenceValue = rollerRenderers[i];
                rollerRenderers[i].enabled = false;
                EditorUtility.SetDirty(rollerRenderers[i]);
            }
        }

        toolSo.ApplyModifiedPropertiesWithoutUndo();

        PlayerController controller = player.GetComponent<PlayerController>();

        if (controller != null)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("toolSwitcher").objectReferenceValue = toolSwitcher;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        EditorUtility.SetDirty(toolSwitcher);
    }

    private static void ConfigurePlayerInkResource(GameObject player)
    {
        InkWeapon weapon = player != null ? player.GetComponentInChildren<InkWeapon>() : null;

        if (weapon == null)
        {
            return;
        }

        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("useInkResource").boolValue = true;
        weaponSo.FindProperty("maxInk").floatValue = 100f;
        weaponSo.FindProperty("inkPerShot").floatValue = 10f;
        weaponSo.FindProperty("inkRecoveryPerSecond").floatValue = 12f;
        weaponSo.FindProperty("ownPaintRecoveryMultiplier").floatValue = 3.5f;
        weaponSo.FindProperty("startWithFullInk").boolValue = true;
        weaponSo.FindProperty("groundProbe").objectReferenceValue = player.transform;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(weapon);
    }

    private static void ConfigurePlayerSwimForm(GameObject player, Material teamAMaterial)
    {
        if (player == null)
        {
            return;
        }

        PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
        PlayerController controller = player.GetComponent<PlayerController>();
        MeshRenderer humanoidRenderer = player.GetComponent<MeshRenderer>();
        GameObject swimFormVisual = GetOrCreateSwimFormVisual(player.transform, teamAMaterial);

        if (input != null)
        {
            SerializedObject inputSo = new SerializedObject(input);
            inputSo.FindProperty("swimKey").intValue = (int)KeyCode.LeftShift;
            inputSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(input);
        }

        if (controller == null)
        {
            return;
        }

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("playerTeam").enumValueIndex = (int)Team.TeamA;
        controllerSo.FindProperty("swimMoveSpeedMultiplier").floatValue = 1.55f;
        controllerSo.FindProperty("enemyPaintMoveSpeedMultiplier").floatValue = 0.55f;
        controllerSo.FindProperty("swimInkRecoveryMultiplier").floatValue = 1.8f;
        controllerSo.FindProperty("disableFireWhileSwimming").boolValue = true;
        controllerSo.FindProperty("groundProbe").objectReferenceValue = player.transform;
        controllerSo.FindProperty("swimFormVisual").objectReferenceValue = swimFormVisual;
        controllerSo.FindProperty("enablePaintRoutes").boolValue = true;
        controllerSo.FindProperty("paintRouteProbeRadius").floatValue = 0.75f;
        controllerSo.FindProperty("paintRouteProbeOffset").vector3Value = new Vector3(0f, 0.45f, 0f);

        SerializedProperty humanoidRenderersProperty = controllerSo.FindProperty("humanoidRenderers");
        humanoidRenderersProperty.ClearArray();

        if (humanoidRenderer != null)
        {
            humanoidRenderersProperty.InsertArrayElementAtIndex(0);
            humanoidRenderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = humanoidRenderer;
        }

        controllerSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static GameObject GetOrCreateSwimFormVisual(Transform parent, Material teamAMaterial)
    {
        Transform existing = parent.Find("SwimFormVisual");
        GameObject swimFormVisual = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        swimFormVisual.name = "SwimFormVisual";
        swimFormVisual.transform.SetParent(parent, false);
        swimFormVisual.transform.localPosition = new Vector3(0f, -0.78f, 0f);
        swimFormVisual.transform.localRotation = Quaternion.identity;
        swimFormVisual.transform.localScale = new Vector3(1.25f, 0.22f, 1.25f);

        Collider collider = swimFormVisual.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        AssignMaterial(swimFormVisual, teamAMaterial);
        swimFormVisual.SetActive(false);
        EditorUtility.SetDirty(swimFormVisual);
        return swimFormVisual;
    }

    private static GameObject GetOrCreateRollerTool(Transform parent, Material teamAMaterial)
    {
        Transform existing = parent.Find("RollerTool");
        GameObject rollerTool = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
        rollerTool.name = "RollerTool";
        rollerTool.transform.SetParent(parent, false);
        rollerTool.transform.localPosition = new Vector3(0f, -0.55f, 0.9f);
        rollerTool.transform.localRotation = Quaternion.identity;
        rollerTool.transform.localScale = new Vector3(1.55f, 0.2f, 0.35f);

        Collider collider = rollerTool.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        AssignMaterial(rollerTool, teamAMaterial);
        return rollerTool;
    }

    private static CharacterHealth GetOrCreateCharacterHealth(GameObject character)
    {
        CharacterHealth health = character != null ? character.GetComponent<CharacterHealth>() : null;

        if (health == null && character != null)
        {
            health = character.AddComponent<CharacterHealth>();
        }

        return health;
    }

    private static void ConfigureCharacterController(GameObject character)
    {
        CharacterController characterController = character != null ? character.GetComponent<CharacterController>() : null;

        if (characterController == null)
        {
            return;
        }

        characterController.height = CharacterControllerHeight;
        characterController.radius = CharacterControllerRadius;
        characterController.center = Vector3.zero;
        characterController.stepOffset = 0.24f;
        EditorUtility.SetDirty(characterController);
    }

    private static void ConfigureCharacterHealth(CharacterHealth health, Team team, Transform groundProbe)
    {
        if (health == null)
        {
            return;
        }

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)team;
        healthSo.FindProperty("maxHealth").floatValue = 100f;
        healthSo.FindProperty("enemyPaintDamagePerSecond").floatValue = 35f;
        healthSo.FindProperty("damageOnlyDuringMatch").boolValue = true;
        healthSo.FindProperty("groundProbe").objectReferenceValue = groundProbe;
        healthSo.FindProperty("hideRenderersWhileEliminated").boolValue = true;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(health);
    }

    private static void PositionExistingCameraForGrayboxMap()
    {
        Camera camera = Camera.main;
        GameObject player = GameObject.Find("Player");

        if (camera == null || player == null)
        {
            return;
        }

        camera.transform.position = player.transform.position + new Vector3(0f, 4.5f, -6f);
        camera.transform.rotation = Quaternion.LookRotation(
            (player.transform.position + Vector3.up * 1.25f) - camera.transform.position,
            Vector3.up);

        EditorUtility.SetDirty(camera);
    }

    private static void ConfigureGameManagerForMatchFlow()
    {
        GameManager gameManager = Object.FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        BotController bot = Object.FindObjectOfType<BotController>();
        PaintManager paintManager = Object.FindObjectOfType<PaintManager>();
        SplatZoneObjective centerZone = Object.FindObjectOfType<SplatZoneObjective>();
        TowerObjective centerTower = Object.FindObjectOfType<TowerObjective>();
        SpawnPoint teamASpawn = FindDefaultSpawnPoint(Team.TeamA);
        SpawnPoint teamBSpawn = FindDefaultSpawnPoint(Team.TeamB);

        SerializedObject managerSo = new SerializedObject(gameManager);
        managerSo.FindProperty("startMatchOnAwake").boolValue = true;
        managerSo.FindProperty("clearPaintOnMatchStart").boolValue = true;
        managerSo.FindProperty("resetCharactersOnMatchStart").boolValue = true;
        managerSo.FindProperty("destroyProjectilesOnMatchStart").boolValue = true;
        managerSo.FindProperty("matchMode").enumValueIndex = (int)GameManager.MatchMode.TurfWar;
        managerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        managerSo.FindProperty("playerRoot").objectReferenceValue = player != null ? player.transform : null;
        managerSo.FindProperty("playerController").objectReferenceValue = player != null ? player.GetComponent<PlayerController>() : null;
        managerSo.FindProperty("playerHealth").objectReferenceValue = player != null ? player.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("playerWeapon").objectReferenceValue = player != null ? player.GetComponentInChildren<InkWeapon>() : null;
        managerSo.FindProperty("playerToolSwitcher").objectReferenceValue = player != null ? player.GetComponent<PlayerToolSwitcher>() : null;
        managerSo.FindProperty("playerSpecialMeter").objectReferenceValue = player != null ? player.GetComponentInChildren<SpecialMeter>() : null;
        managerSo.FindProperty("centerZoneObjective").objectReferenceValue = centerZone;
        managerSo.FindProperty("centerTowerObjective").objectReferenceValue = centerTower;
        managerSo.FindProperty("teamBBot").objectReferenceValue = bot;
        managerSo.FindProperty("teamBBotHealth").objectReferenceValue = bot != null ? bot.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("teamASpawn").objectReferenceValue = teamASpawn;
        managerSo.FindProperty("teamBSpawn").objectReferenceValue = teamBSpawn;
        managerSo.FindProperty("autoCreateScoreUI").boolValue = true;
        managerSo.FindProperty("respawnDelaySeconds").floatValue = 2f;
        managerSo.FindProperty("enableKeyboardControls").boolValue = true;
        managerSo.FindProperty("startKey").intValue = (int)KeyCode.Return;
        managerSo.FindProperty("restartKey").intValue = (int)KeyCode.R;
        managerSo.FindProperty("pauseKey").intValue = (int)KeyCode.P;
        managerSo.FindProperty("alternatePauseKey").intValue = (int)KeyCode.Escape;
        managerSo.FindProperty("cycleModeKey").intValue = (int)KeyCode.M;
        managerSo.FindProperty("pauseUsesTimeScale").boolValue = true;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gameManager);
    }

    private static SpawnPoint FindDefaultSpawnPoint(Team team)
    {
        SpawnPoint[] spawnPoints = Object.FindObjectsOfType<SpawnPoint>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            SpawnPoint spawnPoint = spawnPoints[i];

            if (spawnPoint != null && spawnPoint.Team == team && spawnPoint.DefaultForTeam)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static Transform CreateMarker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void AssignTransformArray(SerializedProperty arrayProperty, Transform[] values)
    {
        arrayProperty.ClearArray();

        for (int i = 0; i < values.Length; i++)
        {
            arrayProperty.InsertArrayElementAtIndex(i);
            arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static GameObject CreateSolidCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.rotation = Quaternion.identity;
        cube.transform.localScale = scale;
        AssignMaterial(cube, material);
        MeshRenderer renderer = cube.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        AddPaintBlocker(cube);
        return cube;
    }

    private static GameObject CreateContainmentCollider(string name, Vector3 position, Vector3 size, Transform parent)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.rotation = Quaternion.identity;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
        collider.isTrigger = false;

        GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.BatchingStatic);
        EditorUtility.SetDirty(wall);
        EditorUtility.SetDirty(collider);
        return wall;
    }

    private static GameObject CreateRamp(string name, Vector3 position, Vector3 scale, Vector3 eulerAngles, Material material, Transform parent)
    {
        GameObject ramp = CreateSolidCube(name, position, scale, material, parent);
        ramp.transform.rotation = Quaternion.Euler(eulerAngles);
        return ramp;
    }

    private static void AddPaintBlocker(GameObject target)
    {
        PaintBlocker blocker = target.AddComponent<PaintBlocker>();
        blocker.Configure(true, 0.05f);
        PaintBlockers.Add(blocker);
        EditorUtility.SetDirty(blocker);
    }

    private static void ConfigurePaintableGroundForGrayboxMap()
    {
        PaintableArea area = null;
        GameObject paintableGround = GameObject.Find("PaintableGround");

        if (paintableGround != null)
        {
            area = paintableGround.GetComponent<PaintableArea>();
        }

        if (area == null)
        {
            area = Object.FindObjectOfType<PaintableArea>();
        }

        if (area == null)
        {
            return;
        }

        SerializedObject areaSo = new SerializedObject(area);
        areaSo.FindProperty("areaSize").vector2Value = new Vector2(MapWidth, MapLength);
        areaSo.FindProperty("gridWidth").intValue = PaintGridWidth;
        areaSo.FindProperty("gridHeight").intValue = PaintGridHeight;
        areaSo.FindProperty("resetOnAwake").boolValue = true;
        areaSo.FindProperty("requirePaintPointNearAreaPlane").boolValue = true;
        areaSo.FindProperty("maxPaintPointHeightOffset").floatValue = 0.16f;
        areaSo.FindProperty("rebuildMaskFromPaintBlockersOnAwake").boolValue = true;
        areaSo.FindProperty("clearMaskBeforeBaking").boolValue = true;
        areaSo.FindProperty("drawGizmos").boolValue = false;
        areaSo.FindProperty("drawOnlyWhenSelected").boolValue = true;
        areaSo.FindProperty("drawPaintedCells").boolValue = false;
        areaSo.FindProperty("drawUnpaintedCells").boolValue = false;
        areaSo.FindProperty("drawBlockedCells").boolValue = false;
        areaSo.FindProperty("drawGridLines").boolValue = false;
        areaSo.ApplyModifiedPropertiesWithoutUndo();

        area.ResetPaintableMask(true);

        for (int i = 0; i < PaintBlockers.Count; i++)
        {
            PaintBlocker blocker = PaintBlockers[i];

            if (blocker == null || !blocker.TryGetWorldBounds(out Bounds bounds))
            {
                continue;
            }

            area.SetCellsPaintableByWorldBounds(bounds, false, blocker.BoundsPadding);
        }

        area.ClearPaint();
        ApplyHangarFloorMaterialToPaintableGround(paintableGround);
        EditorUtility.SetDirty(area);
    }

    private static void ApplyHangarFloorMaterialToPaintableGround(GameObject paintableGround)
    {
        if (paintableGround == null)
        {
            return;
        }

        Material floorMaterial = LoadHangarMaterial("mat_floor.mat");

        if (floorMaterial == null)
        {
            return;
        }

        Transform groundVisual = paintableGround.transform.Find("GroundVisual");
        MeshRenderer renderer = groundVisual != null ? groundVisual.GetComponent<MeshRenderer>() : paintableGround.GetComponentInChildren<MeshRenderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = floorMaterial;
        EditorUtility.SetDirty(renderer);
    }

    private static void AssignMaterial(GameObject target, Material material)
    {
        MeshRenderer renderer = target.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void DestroyExistingLevelRoot()
    {
        GameObject existing = GameObject.Find(LevelRootName);

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void EnsureMaterialFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Level"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Level");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials/Teams"))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Teams");
        }
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }
}
