using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only utility that builds the small graybox arena used by the MVP scene.
/// The layout is intentionally compact so one player and one future bot can contest territory quickly.
/// </summary>
public static class SplatFightersGrayboxMapBuilder
{
    private const string ScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string LevelRootName = "LevelRoot";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const float CharacterRootHeight = 1f;
    private static readonly List<PaintBlocker> PaintBlockers = new List<PaintBlocker>();

    [MenuItem("Tools/Splat Fighters/Build Graybox Map V1")]
    public static void BuildIntoMvpScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Built graybox map v1 into the MVP shooting test scene.");
    }

    public static void BuildInCurrentScene()
    {
        EnsureMaterialFolders();
        PaintBlockers.Clear();

        Material boundaryMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Boundary.mat", new Color(0.18f, 0.2f, 0.22f));
        Material coverMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Cover.mat", new Color(0.42f, 0.47f, 0.48f));
        Material platformMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Platform.mat", new Color(0.3f, 0.36f, 0.34f));
        Material rampMaterial = GetOrCreateMaterial("Assets/Materials/Level/MAT_Level_Ramp.mat", new Color(0.45f, 0.42f, 0.36f));
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
        Transform spawnRoot = CreateGroup(levelRoot.transform, "SpawnPoints");
        Transform aiRoot = CreateGroup(levelRoot.transform, "AI");

        BuildBoundaryWalls(boundaryRoot, boundaryMaterial);
        BuildContestObstacles(obstacleRoot, coverRoot, coverMaterial);
        BuildSidePlatforms(platformRoot, platformMaterial, rampMaterial);
        BuildSpawnPoints(spawnRoot, teamAMaterial, teamBMaterial);
        BuildTeamBBot(aiRoot, teamBBotMaterial);
        ConfigurePaintableGroundForGrayboxMap();
        PositionExistingPlayerAtTeamASpawn(teamAPlayerMaterial);
        PositionExistingCameraForGrayboxMap();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void BuildBoundaryWalls(Transform parent, Material material)
    {
        CreateSolidCube("NorthBoundaryRail", new Vector3(0f, 0.55f, 10.25f), new Vector3(20.8f, 1.1f, 0.5f), material, parent);
        CreateSolidCube("SouthBoundaryRail", new Vector3(0f, 0.55f, -10.25f), new Vector3(20.8f, 1.1f, 0.5f), material, parent);
        CreateSolidCube("EastBoundaryRail", new Vector3(10.25f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 20.8f), material, parent);
        CreateSolidCube("WestBoundaryRail", new Vector3(-10.25f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 20.8f), material, parent);

        CreateSolidCube("NorthEastCornerPost", new Vector3(10.25f, 0.8f, 10.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("NorthWestCornerPost", new Vector3(-10.25f, 0.8f, 10.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("SouthEastCornerPost", new Vector3(10.25f, 0.8f, -10.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
        CreateSolidCube("SouthWestCornerPost", new Vector3(-10.25f, 0.8f, -10.25f), new Vector3(0.8f, 1.6f, 0.8f), material, parent);
    }

    private static void BuildContestObstacles(Transform obstacleRoot, Transform coverRoot, Material material)
    {
        CreateSolidCube("CenterContestBlock", new Vector3(0f, 0.5f, 0f), new Vector3(3.6f, 1f, 1.0f), material, obstacleRoot);
        CreateSolidCube("CenterLeftPillar", new Vector3(-2.45f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 0.8f), material, obstacleRoot);
        CreateSolidCube("CenterRightPillar", new Vector3(2.45f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 0.8f), material, obstacleRoot);
        CreateSolidCube("LeftMidCover", new Vector3(-4.4f, 0.45f, -2.2f), new Vector3(1.1f, 0.9f, 3.0f), material, coverRoot);
        CreateSolidCube("RightMidCover", new Vector3(4.4f, 0.45f, 2.2f), new Vector3(1.1f, 0.9f, 3.0f), material, coverRoot);
        CreateSolidCube("TeamAForwardCover", new Vector3(2.6f, 0.4f, -4.6f), new Vector3(2.4f, 0.8f, 0.8f), material, coverRoot);
        CreateSolidCube("TeamBForwardCover", new Vector3(-2.6f, 0.4f, 4.6f), new Vector3(2.4f, 0.8f, 0.8f), material, coverRoot);
        CreateSolidCube("TeamASpawnLeftCover", new Vector3(-3.6f, 0.45f, -7.0f), new Vector3(1.6f, 0.9f, 0.8f), material, coverRoot);
        CreateSolidCube("TeamASpawnRightCover", new Vector3(3.6f, 0.45f, -7.0f), new Vector3(1.6f, 0.9f, 0.8f), material, coverRoot);
        CreateSolidCube("TeamBSpawnLeftCover", new Vector3(-3.6f, 0.45f, 7.0f), new Vector3(1.6f, 0.9f, 0.8f), material, coverRoot);
        CreateSolidCube("TeamBSpawnRightCover", new Vector3(3.6f, 0.45f, 7.0f), new Vector3(1.6f, 0.9f, 0.8f), material, coverRoot);
        CreateSolidCube("LeftLaneLowBlock", new Vector3(-7.2f, 0.35f, -5.4f), new Vector3(2.0f, 0.7f, 1.0f), material, coverRoot);
        CreateSolidCube("RightLaneLowBlock", new Vector3(7.2f, 0.35f, 5.4f), new Vector3(2.0f, 0.7f, 1.0f), material, coverRoot);
    }

    private static void BuildSidePlatforms(Transform parent, Material platformMaterial, Material rampMaterial)
    {
        CreateSolidCube("WestSidePlatform", new Vector3(-7.6f, 0.2f, 0f), new Vector3(2.6f, 0.4f, 4.4f), platformMaterial, parent);
        CreateSolidCube("EastSidePlatform", new Vector3(7.6f, 0.2f, 0f), new Vector3(2.6f, 0.4f, 4.4f), platformMaterial, parent);
        CreateSolidCube("WestPlatformCover", new Vector3(-7.6f, 0.75f, 1.45f), new Vector3(1.6f, 0.7f, 0.7f), platformMaterial, parent);
        CreateSolidCube("EastPlatformCover", new Vector3(7.6f, 0.75f, -1.45f), new Vector3(1.6f, 0.7f, 0.7f), platformMaterial, parent);

        CreateRamp("WestPlatformRamp", new Vector3(-5.65f, 0.15f, 0f), new Vector3(2.2f, 0.22f, 3.0f), new Vector3(0f, 0f, -8f), rampMaterial, parent);
        CreateRamp("EastPlatformRamp", new Vector3(5.65f, 0.15f, 0f), new Vector3(2.2f, 0.22f, 3.0f), new Vector3(0f, 0f, 8f), rampMaterial, parent);
    }

    private static void BuildSpawnPoints(Transform parent, Material teamAMaterial, Material teamBMaterial)
    {
        CreateSpawnPoint("TeamASpawn", Team.TeamA, new Vector3(0f, CharacterRootHeight, -7.25f), Vector3.forward, teamAMaterial, parent);
        CreateSpawnPoint("TeamBSpawn", Team.TeamB, new Vector3(0f, CharacterRootHeight, 7.25f), Vector3.back, teamBMaterial, parent);
    }

    private static void BuildTeamBBot(Transform parent, Material teamBMaterial)
    {
        InkProjectile projectilePrefab = AssetDatabase.LoadAssetAtPath<InkProjectile>(ProjectilePrefabPath);

        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bot.name = "TeamBBot";
        bot.transform.SetParent(parent, false);
        bot.transform.position = new Vector3(0f, CharacterRootHeight, 6.25f);
        bot.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        CapsuleCollider capsuleCollider = bot.GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            Object.DestroyImmediate(capsuleCollider);
        }

        CharacterController characterController = bot.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = Vector3.zero;
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;

        AssignMaterial(bot, teamBMaterial);
        TeamVisualBinder visualBinder = bot.AddComponent<TeamVisualBinder>();
        visualBinder.Configure(Team.TeamB, null, teamBMaterial);

        GameObject firePointObject = new GameObject("TeamBBotFirePoint");
        firePointObject.transform.SetParent(bot.transform, false);
        firePointObject.transform.localPosition = new Vector3(0f, 0.35f, 0.7f);
        firePointObject.transform.localRotation = Quaternion.identity;

        Transform patrolRoot = CreateGroup(parent, "TeamBBotPatrolPoints");
        Transform[] waypoints =
        {
            CreateMarker("TeamBBotPatrol_01", new Vector3(0f, 1f, 5.8f), patrolRoot),
            CreateMarker("TeamBBotPatrol_02", new Vector3(-3.5f, 1f, 3.2f), patrolRoot),
            CreateMarker("TeamBBotPatrol_03", new Vector3(0f, 1f, 1.4f), patrolRoot),
            CreateMarker("TeamBBotPatrol_04", new Vector3(3.5f, 1f, 3.2f), patrolRoot)
        };

        Transform paintTargetRoot = CreateGroup(parent, "TeamBBotPaintTargets");
        Transform[] paintTargets =
        {
            CreateMarker("TeamBBotPaintTarget_Center", new Vector3(0f, 0f, 1.8f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_LeftLane", new Vector3(-4f, 0f, 0.8f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_RightLane", new Vector3(4f, 0f, -0.8f), paintTargetRoot),
            CreateMarker("TeamBBotPaintTarget_TeamASide", new Vector3(0f, 0f, -3.8f), paintTargetRoot)
        };

        InkWeapon weapon = bot.AddComponent<InkWeapon>();
        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePointObject.transform;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamB;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = 1.6f;
        weaponSo.FindProperty("fireCooldown").floatValue = 0.35f;
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
        botSo.FindProperty("firePoint").objectReferenceValue = firePointObject.transform;
        AssignTransformArray(botSo.FindProperty("waypoints"), waypoints);
        AssignTransformArray(botSo.FindProperty("paintTargets"), paintTargets);
        botSo.FindProperty("moveSpeed").floatValue = 3.2f;
        botSo.FindProperty("turnSpeed").floatValue = 540f;
        botSo.FindProperty("waypointReachDistance").floatValue = 0.6f;
        botSo.FindProperty("fireInterval").floatValue = 0.65f;
        botSo.FindProperty("aimRefreshInterval").floatValue = 1.2f;
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
        AssignMaterial(player, teamAMaterial);
        TeamVisualBinder visualBinder = player.GetComponent<TeamVisualBinder>();

        if (visualBinder == null)
        {
            visualBinder = player.AddComponent<TeamVisualBinder>();
        }

        visualBinder.Configure(Team.TeamA, teamAMaterial, null);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(visualBinder);
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
        AddPaintBlocker(cube);
        return cube;
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
        areaSo.FindProperty("areaSize").vector2Value = new Vector2(20f, 20f);
        areaSo.FindProperty("gridWidth").intValue = 60;
        areaSo.FindProperty("gridHeight").intValue = 60;
        areaSo.FindProperty("resetOnAwake").boolValue = true;
        areaSo.FindProperty("requirePaintPointNearAreaPlane").boolValue = true;
        areaSo.FindProperty("maxPaintPointHeightOffset").floatValue = 0.16f;
        areaSo.FindProperty("rebuildMaskFromPaintBlockersOnAwake").boolValue = true;
        areaSo.FindProperty("clearMaskBeforeBaking").boolValue = true;
        areaSo.FindProperty("drawBlockedCells").boolValue = false;
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
        EditorUtility.SetDirty(area);
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
