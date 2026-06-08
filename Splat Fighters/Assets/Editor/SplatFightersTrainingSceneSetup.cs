using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor-only utility that creates the interactive How to Play training scene.
/// </summary>
public static class SplatFightersTrainingSceneSetup
{
    private const string TrainingScenePath = "Assets/Scenes/HowToPlayTraining.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const string MaterialsFolder = "Assets/Materials";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const float TrainingGroundWidth = 18f;
    private const float TrainingGroundLength = 18f;
    private const int PaintGridWidth = 54;
    private const int PaintGridHeight = 54;
    private const float CharacterRootHeight = 0.8f;
    private const float CharacterControllerHeight = 1.6f;
    private const float CharacterControllerRadius = 0.4f;

    [MenuItem("Tools/Splat Fighters/Create How To Play Training Scene")]
    public static void CreateHowToPlayTrainingScene()
    {
        EnsureFolders();

        Material groundMaterial = GetOrCreateMaterial("Assets/Materials/Training/MAT_Training_Ground.mat", new Color(0.16f, 0.17f, 0.2f));
        Material boundaryMaterial = GetOrCreateMaterial("Assets/Materials/Training/MAT_Training_Boundary.mat", new Color(0.05f, 0.06f, 0.08f));
        Material coverMaterial = GetOrCreateMaterial("Assets/Materials/Training/MAT_Training_Cover.mat", new Color(0.42f, 0.45f, 0.5f));
        Material accentMaterial = GetOrCreateMaterial("Assets/Materials/Training/MAT_Training_Accent.mat", new Color(1f, 0.78f, 0.08f));
        Material playerMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamA_Player.mat", TeamVisualPalette.TeamAColor);
        Material projectileMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamA_Projectile.mat", TeamVisualPalette.TeamAColor);
        InkProjectile projectilePrefab = CreateOrUpdateProjectilePrefab(projectileMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "HowToPlayTraining";

        CreateLighting();
        Camera camera = CreateCamera();
        PaintManager paintManager = CreatePaintManager();
        CreatePaintableGround(groundMaterial);
        GameObject player = CreatePlayer(playerMaterial, projectilePrefab, camera);
        CreateTrainingMap(boundaryMaterial, coverMaterial, accentMaterial);
        SpawnPoint spawnPoint = CreateSpawnPoint(playerMaterial);
        CreateGameManager(paintManager, player, spawnPoint);
        ConfigureCameraFollow(camera, player.transform);
        CreateTrainingLessonCanvas(player, paintManager);
        CreateEventSystem();
        SplatFightersActorPrefabSetup.ApplyTrainingActorPrefabsInCurrentScene();

        EditorSceneManager.SaveScene(scene, TrainingScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created How to Play training scene at {TrainingScenePath}.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");
        EnsureFolder(MaterialsFolder, "Teams");
        EnsureFolder(MaterialsFolder, "Training");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PrefabsFolder, "Weapons");
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
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

    private static InkProjectile CreateOrUpdateProjectilePrefab(Material projectileMaterial)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "InkProjectile";
        projectileObject.transform.localScale = Vector3.one * 0.25f;

        MeshRenderer renderer = projectileObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = projectileMaterial;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        InkProjectile projectile = projectileObject.AddComponent<InkProjectile>();
        SerializedObject projectileSo = new SerializedObject(projectile);
        projectileSo.FindProperty("defaultSpeed").floatValue = 18f;
        projectileSo.FindProperty("lifetime").floatValue = 4f;
        projectileSo.FindProperty("paintRadius").floatValue = 1.75f;
        projectileSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        projectileSo.FindProperty("destroyOnNonPaintableHit").boolValue = true;
        projectileSo.FindProperty("useSweepHitDetection").boolValue = true;
        projectileSo.FindProperty("sweepRadius").floatValue = 0.12f;
        projectileSo.FindProperty("sweepExtraDistance").floatValue = 0.08f;
        projectileSo.FindProperty("useIntendedImpactPoint").boolValue = true;
        projectileSo.FindProperty("paintAtIntendedImpactPoint").boolValue = true;
        projectileSo.FindProperty("intendedImpactPointTolerance").floatValue = 0.35f;
        projectileSo.FindProperty("spawnImpactMarker").boolValue = false;
        projectileSo.FindProperty("spawnInkSplatterVfx").boolValue = true;
        projectileSo.FindProperty("spawnSplatterOnNonPaintableHit").boolValue = false;
        projectileSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ProjectilePrefabPath);
        Object.DestroyImmediate(projectileObject);
        return prefab.GetComponent<InkProjectile>();
    }

    private static void CreateLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.74f, 0.78f, 0.84f, 1f);
        RenderSettings.ambientIntensity = 1.1f;

        GameObject lightObject = new GameObject("Training Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.9f;
        light.shadows = LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(56f, -28f, 0f);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 5f, -8f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, -2f) - cameraObject.transform.position);
        return camera;
    }

    private static PaintManager CreatePaintManager()
    {
        GameObject managerObject = new GameObject("PaintManager");
        return managerObject.AddComponent<PaintManager>();
    }

    private static void CreatePaintableGround(Material groundMaterial)
    {
        GameObject groundRoot = new GameObject("TrainingPaintableGround");
        groundRoot.transform.position = Vector3.zero;

        PaintableArea area = groundRoot.AddComponent<PaintableArea>();
        SerializedObject areaSo = new SerializedObject(area);
        areaSo.FindProperty("areaSize").vector2Value = new Vector2(TrainingGroundWidth, TrainingGroundLength);
        areaSo.FindProperty("gridWidth").intValue = PaintGridWidth;
        areaSo.FindProperty("gridHeight").intValue = PaintGridHeight;
        areaSo.FindProperty("resetOnAwake").boolValue = true;
        areaSo.FindProperty("requirePaintPointNearAreaPlane").boolValue = true;
        areaSo.FindProperty("maxPaintPointHeightOffset").floatValue = 0.16f;
        areaSo.FindProperty("rebuildMaskFromPaintBlockersOnAwake").boolValue = true;
        areaSo.FindProperty("clearMaskBeforeBaking").boolValue = true;
        areaSo.FindProperty("drawGizmos").boolValue = false;
        areaSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "TrainingGroundVisual";
        visual.transform.SetParent(groundRoot.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        visual.transform.localScale = new Vector3(TrainingGroundWidth, 0.1f, TrainingGroundLength);
        AssignMaterial(visual, groundMaterial);
    }

    private static GameObject CreatePlayer(Material playerMaterial, InkProjectile projectilePrefab, Camera camera)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, CharacterRootHeight, -6.2f);

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        Object.DestroyImmediate(capsuleCollider);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = CharacterControllerHeight;
        characterController.radius = CharacterControllerRadius;
        characterController.center = Vector3.zero;
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.24f;

        MeshRenderer renderer = player.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = playerMaterial;

        GameObject swimFormVisual = CreateSwimFormVisual(player.transform, playerMaterial);
        Transform firePoint = CreateFirePoint(player.transform);
        RollerPaintTool rollerPaintTool = CreateRollerTool(player.transform, playerMaterial);
        Renderer rollerRenderer = rollerPaintTool.GetComponent<Renderer>();

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        PlayerController playerController = player.AddComponent<PlayerController>();
        CharacterHealth health = player.AddComponent<CharacterHealth>();
        InkWeapon weapon = player.AddComponent<InkWeapon>();
        SpecialMeter specialMeter = player.AddComponent<SpecialMeter>();
        SpecialPaintBurst specialPaintBurst = player.AddComponent<SpecialPaintBurst>();
        PlayerToolSwitcher toolSwitcher = player.AddComponent<PlayerToolSwitcher>();
        AimController aimController = player.AddComponent<AimController>();
        TeamVisualBinder visualBinder = player.AddComponent<TeamVisualBinder>();
        visualBinder.Configure(Team.TeamA, playerMaterial, null);

        SerializedObject inputSo = new SerializedObject(input);
        inputSo.FindProperty("swimKey").intValue = (int)KeyCode.LeftShift;
        inputSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        healthSo.FindProperty("maxHealth").floatValue = 100f;
        healthSo.FindProperty("enemyPaintDamagePerSecond").floatValue = 0f;
        healthSo.FindProperty("damageOnlyDuringMatch").boolValue = true;
        healthSo.FindProperty("groundProbe").objectReferenceValue = player.transform;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSo = new SerializedObject(playerController);
        controllerSo.FindProperty("cameraTransform").objectReferenceValue = camera.transform;
        controllerSo.FindProperty("weapon").objectReferenceValue = weapon;
        controllerSo.FindProperty("aimController").objectReferenceValue = aimController;
        controllerSo.FindProperty("toolSwitcher").objectReferenceValue = toolSwitcher;
        controllerSo.FindProperty("moveSpeed").floatValue = 6f;
        controllerSo.FindProperty("rotationSpeed").floatValue = 720f;
        controllerSo.FindProperty("rotationMode").enumValueIndex = 2;
        controllerSo.FindProperty("playerTeam").enumValueIndex = (int)Team.TeamA;
        controllerSo.FindProperty("swimMoveSpeedMultiplier").floatValue = 1.55f;
        controllerSo.FindProperty("enemyPaintMoveSpeedMultiplier").floatValue = 0.55f;
        controllerSo.FindProperty("swimInkRecoveryMultiplier").floatValue = 1.8f;
        controllerSo.FindProperty("disableFireWhileSwimming").boolValue = true;
        controllerSo.FindProperty("groundProbe").objectReferenceValue = player.transform;
        controllerSo.FindProperty("swimFormVisual").objectReferenceValue = swimFormVisual;
        SerializedProperty humanoidRenderersProperty = controllerSo.FindProperty("humanoidRenderers");
        humanoidRenderersProperty.ClearArray();
        humanoidRenderersProperty.InsertArrayElementAtIndex(0);
        humanoidRenderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        controllerSo.FindProperty("enableJump").boolValue = true;
        controllerSo.FindProperty("jumpHeight").floatValue = 1.2f;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePoint;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = 1.65f;
        weaponSo.FindProperty("fireCooldown").floatValue = 0.18f;
        weaponSo.FindProperty("useInkResource").boolValue = true;
        weaponSo.FindProperty("maxInk").floatValue = 100f;
        weaponSo.FindProperty("inkPerShot").floatValue = 8f;
        weaponSo.FindProperty("inkRecoveryPerSecond").floatValue = 15f;
        weaponSo.FindProperty("ownPaintRecoveryMultiplier").floatValue = 4f;
        weaponSo.FindProperty("startWithFullInk").boolValue = true;
        weaponSo.FindProperty("groundProbe").objectReferenceValue = player.transform;
        weaponSo.FindProperty("useCameraAim").boolValue = false;
        weaponSo.FindProperty("paintDirectlyAtAimTarget").boolValue = true;
        weaponSo.FindProperty("projectileIsVisualOnlyWhenDirectPainting").boolValue = true;
        weaponSo.FindProperty("applyTeamColorToProjectile").boolValue = true;
        weaponSo.FindProperty("teamAProjectileColor").colorValue = TeamVisualPalette.TeamAColor;
        weaponSo.FindProperty("teamBProjectileColor").colorValue = TeamVisualPalette.TeamBColor;
        weaponSo.FindProperty("enableKeyboardTestFire").boolValue = false;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject specialSo = new SerializedObject(specialMeter);
        specialSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        specialSo.FindProperty("changedCellsForFullCharge").intValue = 70;
        specialSo.FindProperty("startingChargePercent").floatValue = 0f;
        specialSo.FindProperty("resetWhenPaintCleared").boolValue = true;
        specialSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject burstSo = new SerializedObject(specialPaintBurst);
        burstSo.FindProperty("specialMeter").objectReferenceValue = specialMeter;
        burstSo.FindProperty("aimController").objectReferenceValue = aimController;
        burstSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        burstSo.FindProperty("burstPaintRadius").floatValue = 3.2f;
        burstSo.FindProperty("fallbackDistance").floatValue = 4.5f;
        burstSo.FindProperty("activationKey").intValue = (int)KeyCode.Q;
        burstSo.FindProperty("requireMatchPlaying").boolValue = true;
        burstSo.FindProperty("spawnInkSplatterVfx").boolValue = true;
        burstSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject rollerSo = new SerializedObject(rollerPaintTool);
        rollerSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        rollerSo.FindProperty("paintKey").intValue = (int)KeyCode.Mouse0;
        rollerSo.FindProperty("requireInput").boolValue = true;
        rollerSo.FindProperty("requireMatchPlaying").boolValue = true;
        rollerSo.FindProperty("paintOrigin").objectReferenceValue = player.transform;
        rollerSo.FindProperty("paintInterval").floatValue = 0.08f;
        rollerSo.FindProperty("paintRadius").floatValue = 0.95f;
        rollerSo.FindProperty("forwardOffset").floatValue = 1.1f;
        rollerSo.FindProperty("halfWidth").floatValue = 0.6f;
        rollerSo.FindProperty("swathSamples").intValue = 3;
        rollerSo.ApplyModifiedPropertiesWithoutUndo();
        rollerPaintTool.enabled = false;

        SerializedObject toolSo = new SerializedObject(toolSwitcher);
        toolSo.FindProperty("defaultTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("currentTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("shooter").objectReferenceValue = weapon;
        toolSo.FindProperty("roller").objectReferenceValue = rollerPaintTool;
        toolSo.FindProperty("enableKeyboardSwitching").boolValue = true;
        SerializedProperty rollerRenderersProperty = toolSo.FindProperty("rollerRenderers");
        rollerRenderersProperty.ClearArray();
        rollerRenderersProperty.InsertArrayElementAtIndex(0);
        rollerRenderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = rollerRenderer;
        toolSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject aimSo = new SerializedObject(aimController);
        aimSo.FindProperty("aimCamera").objectReferenceValue = camera;
        aimSo.FindProperty("weapon").objectReferenceValue = weapon;
        aimSo.FindProperty("firePoint").objectReferenceValue = firePoint;
        aimSo.FindProperty("characterRoot").objectReferenceValue = player.transform;
        aimSo.FindProperty("weaponPivot").objectReferenceValue = firePoint;
        aimSo.FindProperty("ignoredRoot").objectReferenceValue = player.transform;
        aimSo.FindProperty("autoCreateReticle").boolValue = true;
        aimSo.FindProperty("aimInputMode").enumValueIndex = 0;
        aimSo.FindProperty("maxAimDistance").floatValue = 100f;
        aimSo.FindProperty("rotateCharacterToAim").boolValue = true;
        aimSo.FindProperty("rotateWeaponPivotToAim").boolValue = true;
        aimSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(player);
        return player;
    }

    private static Transform CreateFirePoint(Transform parent)
    {
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(parent);
        firePoint.transform.position = new Vector3(0f, 1.1f, -5.5f);
        firePoint.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        return firePoint.transform;
    }

    private static GameObject CreateSwimFormVisual(Transform parent, Material material)
    {
        GameObject swimFormVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        swimFormVisual.name = "SwimFormVisual";
        swimFormVisual.transform.SetParent(parent, false);
        swimFormVisual.transform.localPosition = new Vector3(0f, -0.78f, 0f);
        swimFormVisual.transform.localRotation = Quaternion.identity;
        swimFormVisual.transform.localScale = new Vector3(1.25f, 0.22f, 1.25f);
        DestroyCollider(swimFormVisual);
        AssignMaterial(swimFormVisual, material);
        swimFormVisual.SetActive(false);
        return swimFormVisual;
    }

    private static RollerPaintTool CreateRollerTool(Transform parent, Material material)
    {
        GameObject rollerTool = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rollerTool.name = "RollerTool";
        rollerTool.transform.SetParent(parent, false);
        rollerTool.transform.localPosition = new Vector3(0f, -0.55f, 0.9f);
        rollerTool.transform.localRotation = Quaternion.identity;
        rollerTool.transform.localScale = new Vector3(1.55f, 0.2f, 0.35f);
        DestroyCollider(rollerTool);
        AssignMaterial(rollerTool, material);
        return rollerTool.AddComponent<RollerPaintTool>();
    }

    private static void CreateTrainingMap(Material boundaryMaterial, Material coverMaterial, Material accentMaterial)
    {
        GameObject root = new GameObject("TrainingMap");
        Transform boundaryRoot = CreateGroup(root.transform, "Boundary");
        Transform practiceRoot = CreateGroup(root.transform, "PracticeProps");

        float halfWidth = TrainingGroundWidth * 0.5f;
        float halfLength = TrainingGroundLength * 0.5f;
        CreateSolidCube("NorthTrainingWall", new Vector3(0f, 0.65f, halfLength + 0.25f), new Vector3(TrainingGroundWidth + 0.8f, 1.3f, 0.5f), boundaryMaterial, boundaryRoot);
        CreateSolidCube("SouthTrainingWall", new Vector3(0f, 0.65f, -halfLength - 0.25f), new Vector3(TrainingGroundWidth + 0.8f, 1.3f, 0.5f), boundaryMaterial, boundaryRoot);
        CreateSolidCube("EastTrainingWall", new Vector3(halfWidth + 0.25f, 0.65f, 0f), new Vector3(0.5f, 1.3f, TrainingGroundLength + 0.8f), boundaryMaterial, boundaryRoot);
        CreateSolidCube("WestTrainingWall", new Vector3(-halfWidth - 0.25f, 0.65f, 0f), new Vector3(0.5f, 1.3f, TrainingGroundLength + 0.8f), boundaryMaterial, boundaryRoot);

        CreateSolidCube("CenterTargetBlock", new Vector3(0f, 0.45f, 1.8f), new Vector3(2.2f, 0.9f, 0.8f), coverMaterial, practiceRoot);
        CreateSolidCube("LeftPaintPracticeBlock", new Vector3(-4.2f, 0.4f, -0.5f), new Vector3(1.4f, 0.8f, 2.4f), coverMaterial, practiceRoot);
        CreateSolidCube("RightPaintPracticeBlock", new Vector3(4.2f, 0.4f, -0.5f), new Vector3(1.4f, 0.8f, 2.4f), coverMaterial, practiceRoot);
        CreateSolidCube("SpecialTargetPad", new Vector3(0f, 0.04f, 4.9f), new Vector3(4.4f, 0.08f, 2.6f), accentMaterial, practiceRoot, true);
        CreateTrainingDummy(practiceRoot, accentMaterial);
        CreateWorldLabel("TrainingLabel_Move", "MOVE + AIM", new Vector3(-4.3f, 0.08f, -6.6f), practiceRoot, TeamVisualPalette.TeamAColor);
        CreateWorldLabel("TrainingLabel_Paint", "PAINT HERE", new Vector3(3.6f, 0.08f, -2.6f), practiceRoot, new Color(1f, 0.78f, 0.08f));
        CreateWorldLabel("TrainingLabel_Special", "SPECIAL TARGET", new Vector3(0f, 0.08f, 4.9f), practiceRoot, new Color(1f, 0.2f, 0.65f));
    }

    private static SpawnPoint CreateSpawnPoint(Material playerMaterial)
    {
        GameObject spawnObject = new GameObject("TrainingSpawn_TeamA");
        spawnObject.transform.position = new Vector3(0f, CharacterRootHeight, -6.2f);
        spawnObject.transform.rotation = Quaternion.identity;
        SpawnPoint spawnPoint = spawnObject.AddComponent<SpawnPoint>();
        spawnPoint.Configure(Team.TeamA, true);

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "SpawnPadVisual";
        pad.transform.SetParent(spawnObject.transform, false);
        pad.transform.localPosition = new Vector3(0f, -0.78f, 0f);
        pad.transform.localScale = new Vector3(1.4f, 0.05f, 1.4f);
        DestroyCollider(pad);
        AssignMaterial(pad, playerMaterial);
        return spawnPoint;
    }

    private static void CreateGameManager(PaintManager paintManager, GameObject player, SpawnPoint spawnPoint)
    {
        GameObject managerObject = new GameObject("GameManager");
        GameManager gameManager = managerObject.AddComponent<GameManager>();
        PerformanceProfile performanceProfile = managerObject.AddComponent<PerformanceProfile>();

        SerializedObject managerSo = new SerializedObject(gameManager);
        managerSo.FindProperty("startMatchOnAwake").boolValue = true;
        managerSo.FindProperty("clearPaintOnMatchStart").boolValue = true;
        managerSo.FindProperty("resetCharactersOnMatchStart").boolValue = true;
        managerSo.FindProperty("destroyProjectilesOnMatchStart").boolValue = true;
        managerSo.FindProperty("matchMode").enumValueIndex = (int)GameManager.MatchMode.TurfWar;
        managerSo.FindProperty("matchDurationSeconds").floatValue = 300f;
        managerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        managerSo.FindProperty("playerRoot").objectReferenceValue = player.transform;
        managerSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
        managerSo.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<CharacterHealth>();
        managerSo.FindProperty("playerWeapon").objectReferenceValue = player.GetComponent<InkWeapon>();
        managerSo.FindProperty("playerToolSwitcher").objectReferenceValue = player.GetComponent<PlayerToolSwitcher>();
        managerSo.FindProperty("playerSpecialMeter").objectReferenceValue = player.GetComponent<SpecialMeter>();
        managerSo.FindProperty("teamASpawn").objectReferenceValue = spawnPoint;
        managerSo.FindProperty("autoCreateScoreUI").boolValue = true;
        managerSo.FindProperty("autoCreateResultsUI").boolValue = false;
        managerSo.FindProperty("enableKeyboardControls").boolValue = true;
        managerSo.FindProperty("pauseKey").intValue = (int)KeyCode.P;
        managerSo.FindProperty("alternatePauseKey").intValue = (int)KeyCode.Escape;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject performanceSo = new SerializedObject(performanceProfile);
        performanceSo.FindProperty("targetFrameRate").intValue = 45;
        performanceSo.FindProperty("disableVSync").boolValue = true;
        performanceSo.FindProperty("fixedDeltaTime").floatValue = 0.02f;
        performanceSo.FindProperty("applyOnAwake").boolValue = true;
        performanceSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCameraFollow(Camera camera, Transform target)
    {
        ThirdPersonCameraFollow follow = camera.gameObject.AddComponent<ThirdPersonCameraFollow>();
        SerializedObject followSo = new SerializedObject(follow);
        followSo.FindProperty("target").objectReferenceValue = target;
        followSo.FindProperty("useTargetRotation").boolValue = true;
        followSo.FindProperty("enableMouseOrbit").boolValue = true;
        followSo.FindProperty("lockCursorOnPlay").boolValue = true;
        followSo.FindProperty("shoulderOffset").floatValue = 0.65f;
        followSo.FindProperty("orbitDistance").floatValue = 5.7f;
        followSo.FindProperty("initialPitch").floatValue = 19f;
        followSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTrainingLessonCanvas(GameObject player, PaintManager paintManager)
    {
        GameObject canvasObject = new GameObject("TrainingLessonCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = CreateUiImage(canvasObject.transform, "TrainingLessonPanel", new Vector2(22f, -22f), new Vector2(430f, 230f), new Color(0.025f, 0.031f, 0.046f, 0.88f), TextAnchor.UpperLeft);
        CreateUiStripe(panel.transform, "TrainingPanelCyanStripe", new Vector2(0f, -7f), new Vector2(390f, 9f), TeamVisualPalette.TeamAColor);
        Text titleText = CreateUiText(panel.transform, "LessonTitleText", "Step 1 - Move", new Vector2(24f, -34f), new Vector2(360f, 38f), 26, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        Text bodyText = CreateUiText(panel.transform, "LessonBodyText", "Use WASD and mouse aim to move around the small arena.", new Vector2(24f, -78f), new Vector2(360f, 78f), 21, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.9f, 0.96f, 1f, 1f));
        Text progressText = CreateUiText(panel.transform, "LessonProgressText", "Movement: 0.0/5.0m", new Vector2(24f, -170f), new Vector2(360f, 36f), 21, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.08f, 1f));
        Button backButton = CreateUiButton(canvasObject.transform, "BackToMenuButton", "Back to Menu", new Vector2(-24f, 24f), new Vector2(190f, 52f), new Color(0.16f, 0.18f, 0.23f, 0.96f), TextAnchor.LowerRight);

        TrainingLessonController controller = canvasObject.AddComponent<TrainingLessonController>();
        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
        controllerSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
        controllerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        controllerSo.FindProperty("specialMeter").objectReferenceValue = player.GetComponent<SpecialMeter>();
        controllerSo.FindProperty("titleText").objectReferenceValue = titleText;
        controllerSo.FindProperty("bodyText").objectReferenceValue = bodyText;
        controllerSo.FindProperty("progressText").objectReferenceValue = progressText;
        controllerSo.FindProperty("backToMenuButton").objectReferenceValue = backButton;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject CreateSolidCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool trigger = false)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        AssignMaterial(cube, material);

        Collider collider = cube.GetComponent<Collider>();

        if (collider != null)
        {
            collider.isTrigger = trigger;
        }

        return cube;
    }

    private static void CreateTrainingDummy(Transform parent, Material material)
    {
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = "TrainingTargetDummy";
        dummy.transform.SetParent(parent, false);
        dummy.transform.position = new Vector3(0f, 0.85f, 5.1f);
        dummy.transform.localScale = new Vector3(0.8f, 1.05f, 0.8f);
        AssignMaterial(dummy, material);
    }

    private static void CreateWorldLabel(string name, string label, Vector3 position, Transform parent, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Canvas canvas = labelObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(3.2f, 0.8f);
        labelObject.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        Text text = CreateUiText(labelObject.transform, "Label", label, Vector2.zero, new Vector2(300f, 60f), 28, FontStyle.Bold, TextAnchor.MiddleCenter, color);
        text.raycastTarget = false;
    }

    private static GameObject CreateUiImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, TextAnchor anchor)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        ApplyUiAnchor(rect, anchor);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return imageObject;
    }

    private static void CreateUiStripe(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        CreateUiImage(parent, name, anchoredPosition, size, color, TextAnchor.UpperLeft);
    }

    private static Text CreateUiText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        ApplyUiAnchor(rect, TextAnchor.UpperLeft);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text textComponent = textObject.GetComponent<Text>();
        textComponent.text = text;
        textComponent.color = color;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.resizeTextForBestFit = true;
        textComponent.resizeTextMinSize = 10;
        textComponent.resizeTextMaxSize = fontSize;
        textComponent.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return textComponent;
    }

    private static Button CreateUiButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color, TextAnchor anchor)
    {
        GameObject buttonObject = CreateUiImage(parent, name, anchoredPosition, size, color, anchor);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(Mathf.Min(color.r + 0.08f, 1f), Mathf.Min(color.g + 0.08f, 1f), Mathf.Min(color.b + 0.08f, 1f), color.a);
        colors.pressedColor = new Color(Mathf.Max(color.r - 0.08f, 0f), Mathf.Max(color.g - 0.08f, 0f), Mathf.Max(color.b - 0.08f, 0f), color.a);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateUiText(buttonObject.transform, "Label", label, Vector2.zero, size, 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    private static void ApplyUiAnchor(RectTransform rect, TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                break;
            case TextAnchor.LowerRight:
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                break;
            default:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
        }
    }

    private static void AssignMaterial(GameObject target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void EnsureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true),
            new EditorBuildSettingsScene(TrainingScenePath, true),
        };
    }
}
