using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only setup utility for creating a ready-to-test MVP shooting scene.
/// Run from Tools/Splat Fighters/Create MVP Shooting Test Scene.
/// </summary>
public static class SplatFightersMvpSceneSetup
{
    private const string ScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const string MaterialsFolder = "Assets/Materials";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const float PaintableGroundWidth = 32f;
    private const float PaintableGroundLength = 36f;
    private const int PaintGridWidth = 80;
    private const int PaintGridHeight = 90;
    private const float CharacterRootHeight = 0.8f;
    private const float CharacterControllerHeight = 1.6f;
    private const float CharacterControllerRadius = 0.4f;

    [MenuItem("Tools/Splat Fighters/Create MVP Shooting Test Scene")]
    public static void CreateMvpShootingTestScene()
    {
        EnsureFolders();

        Material groundMaterial = GetOrCreateMaterial("Assets/Materials/MAT_Ground_Debug.mat", new Color(0.35f, 0.35f, 0.35f));
        Material shooterMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamA_Player.mat", TeamVisualPalette.TeamAColor);
        Material projectileMaterial = GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamA_Projectile.mat", TeamVisualPalette.TeamAColor);
        GetOrCreateMaterial("Assets/Materials/Teams/MAT_TeamB_Projectile.mat", TeamVisualPalette.TeamBColor);

        InkProjectile projectilePrefab = CreateOrUpdateProjectilePrefab(projectileMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MVP_ShootingTest";

        CreateLighting();
        Camera camera = CreateCamera();
        PaintManager paintManager = CreatePaintManager();
        CreatePaintableGround(groundMaterial);
        GameObject player = CreatePlayer(shooterMaterial, projectilePrefab, camera);
        CreateGameManager(paintManager);
        ConfigureCameraFollow(camera, player.transform);
        SplatFightersGrayboxMapBuilder.BuildInCurrentScene();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created MVP shooting test scene at {ScenePath}. Press Play, use WASD to move, Space to jump, and hold Mouse0 to shoot ink projectiles.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PrefabsFolder, "Weapons");
        EnsureFolder("Assets", "Materials");
        EnsureFolder(MaterialsFolder, "Teams");
        EnsureFolder("Assets", "Scenes");
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
        projectileSo.FindProperty("impactMarkerColor").colorValue = new Color(0.05f, 0.65f, 1f, 0.85f);
        projectileSo.FindProperty("noPaintMarkerColor").colorValue = new Color(1f, 1f, 1f, 0.45f);
        projectileSo.FindProperty("impactMarkerSize").floatValue = 0.35f;
        projectileSo.FindProperty("impactMarkerLifetime").floatValue = 0.25f;
        projectileSo.FindProperty("impactMarkerSurfaceOffset").floatValue = 0.03f;
        projectileSo.FindProperty("logPaintMisses").boolValue = false;
        projectileSo.FindProperty("spawnInkSplatterVfx").boolValue = true;
        projectileSo.FindProperty("spawnSplatterOnNonPaintableHit").boolValue = true;
        projectileSo.FindProperty("splatterRadiusMultiplier").floatValue = 1.1f;
        projectileSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ProjectilePrefabPath);
        Object.DestroyImmediate(projectileObject);

        return prefab.GetComponent<InkProjectile>();
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 5f, -10f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, -2f) - cameraObject.transform.position);

        return camera;
    }

    private static PaintManager CreatePaintManager()
    {
        GameObject managerObject = new GameObject("PaintManager");
        return managerObject.AddComponent<PaintManager>();
    }

    private static void CreateGameManager(PaintManager paintManager)
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
        managerSo.FindProperty("matchDurationSeconds").floatValue = 180f;
        managerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        managerSo.FindProperty("autoCreateScoreUI").boolValue = true;
        managerSo.FindProperty("respawnDelaySeconds").floatValue = 2f;
        managerSo.FindProperty("scoreRefreshInterval").floatValue = 0.2f;
        managerSo.FindProperty("enableKeyboardControls").boolValue = true;
        managerSo.FindProperty("startKey").intValue = (int)KeyCode.Return;
        managerSo.FindProperty("restartKey").intValue = (int)KeyCode.R;
        managerSo.FindProperty("pauseKey").intValue = (int)KeyCode.P;
        managerSo.FindProperty("alternatePauseKey").intValue = (int)KeyCode.Escape;
        managerSo.FindProperty("cycleModeKey").intValue = (int)KeyCode.M;
        managerSo.FindProperty("pauseUsesTimeScale").boolValue = true;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject performanceSo = new SerializedObject(performanceProfile);
        performanceSo.FindProperty("targetFrameRate").intValue = 60;
        performanceSo.FindProperty("disableVSync").boolValue = true;
        performanceSo.FindProperty("fixedDeltaTime").floatValue = 0.02f;
        performanceSo.FindProperty("applyOnAwake").boolValue = true;
        performanceSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePaintableGround(Material groundMaterial)
    {
        GameObject groundRoot = new GameObject("PaintableGround");
        groundRoot.transform.position = Vector3.zero;

        PaintableArea area = groundRoot.AddComponent<PaintableArea>();
        SerializedObject areaSo = new SerializedObject(area);
        areaSo.FindProperty("areaSize").vector2Value = new Vector2(PaintableGroundWidth, PaintableGroundLength);
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

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "GroundVisual";
        visual.transform.SetParent(groundRoot.transform);
        visual.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        visual.transform.localScale = new Vector3(PaintableGroundWidth, 0.1f, PaintableGroundLength);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = groundMaterial;
    }

    private static GameObject CreatePlayer(Material shooterMaterial, InkProjectile projectilePrefab, Camera camera)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, CharacterRootHeight, -7f);

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        Object.DestroyImmediate(capsuleCollider);

        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = CharacterControllerHeight;
        characterController.radius = CharacterControllerRadius;
        characterController.center = Vector3.zero;
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.24f;

        MeshRenderer renderer = player.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = shooterMaterial;

        GameObject swimFormVisual = CreateSwimFormVisual(player.transform, shooterMaterial);

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.position = new Vector3(0f, 1.1f, -6.3f);
        firePoint.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.05f, 0f) - firePoint.transform.position, Vector3.up);

        GameObject rollerTool = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rollerTool.name = "RollerTool";
        rollerTool.transform.SetParent(player.transform);
        rollerTool.transform.localPosition = new Vector3(0f, -0.55f, 0.9f);
        rollerTool.transform.localRotation = Quaternion.identity;
        rollerTool.transform.localScale = new Vector3(1.55f, 0.2f, 0.35f);

        Collider rollerCollider = rollerTool.GetComponent<Collider>();

        if (rollerCollider != null)
        {
            Object.DestroyImmediate(rollerCollider);
        }

        MeshRenderer rollerRenderer = rollerTool.GetComponent<MeshRenderer>();
        rollerRenderer.sharedMaterial = shooterMaterial;

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        PlayerController playerController = player.AddComponent<PlayerController>();
        CharacterHealth health = player.AddComponent<CharacterHealth>();
        InkWeapon weapon = player.AddComponent<InkWeapon>();
        SpecialMeter specialMeter = player.AddComponent<SpecialMeter>();
        SpecialPaintBurst specialPaintBurst = player.AddComponent<SpecialPaintBurst>();
        RollerPaintTool rollerPaintTool = rollerTool.AddComponent<RollerPaintTool>();
        PlayerToolSwitcher toolSwitcher = player.AddComponent<PlayerToolSwitcher>();
        AimController aimController = player.AddComponent<AimController>();
        TeamVisualBinder visualBinder = player.AddComponent<TeamVisualBinder>();
        visualBinder.Configure(Team.TeamA, shooterMaterial, null);

        ConfigureCharacterHealth(health, Team.TeamA, player.transform);

        SerializedObject inputSo = new SerializedObject(input);
        inputSo.FindProperty("swimKey").intValue = (int)KeyCode.LeftShift;
        inputSo.ApplyModifiedPropertiesWithoutUndo();

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
        controllerSo.FindProperty("enablePaintRoutes").boolValue = true;
        controllerSo.FindProperty("paintRouteProbeRadius").floatValue = 0.75f;
        controllerSo.FindProperty("paintRouteProbeOffset").vector3Value = new Vector3(0f, 0.45f, 0f);
        SerializedProperty humanoidRenderersProperty = controllerSo.FindProperty("humanoidRenderers");
        humanoidRenderersProperty.ClearArray();
        humanoidRenderersProperty.InsertArrayElementAtIndex(0);
        humanoidRenderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        controllerSo.FindProperty("enableJump").boolValue = true;
        controllerSo.FindProperty("jumpHeight").floatValue = 1.2f;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = 1.75f;
        weaponSo.FindProperty("fireCooldown").floatValue = 0.2f;
        weaponSo.FindProperty("useInkResource").boolValue = true;
        weaponSo.FindProperty("maxInk").floatValue = 100f;
        weaponSo.FindProperty("inkPerShot").floatValue = 10f;
        weaponSo.FindProperty("inkRecoveryPerSecond").floatValue = 12f;
        weaponSo.FindProperty("ownPaintRecoveryMultiplier").floatValue = 3.5f;
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
        specialSo.FindProperty("changedCellsForFullCharge").intValue = 180;
        specialSo.FindProperty("startingChargePercent").floatValue = 0f;
        specialSo.FindProperty("resetWhenPaintCleared").boolValue = true;
        specialSo.ApplyModifiedPropertiesWithoutUndo();

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
        rollerRenderersProperty.InsertArrayElementAtIndex(0);
        rollerRenderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = rollerRenderer;
        toolSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject aimSo = new SerializedObject(aimController);
        aimSo.FindProperty("aimCamera").objectReferenceValue = camera;
        aimSo.FindProperty("weapon").objectReferenceValue = weapon;
        aimSo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        aimSo.FindProperty("characterRoot").objectReferenceValue = player.transform;
        aimSo.FindProperty("weaponPivot").objectReferenceValue = firePoint.transform;
        aimSo.FindProperty("ignoredRoot").objectReferenceValue = player.transform;
        aimSo.FindProperty("autoCreateReticle").boolValue = true;
        aimSo.FindProperty("aimInputMode").enumValueIndex = 0;
        aimSo.FindProperty("maxAimDistance").floatValue = 100f;
        aimSo.FindProperty("minimumAimDistance").floatValue = 0.05f;
        aimSo.FindProperty("ignoreProjectiles").boolValue = true;
        aimSo.FindProperty("rotateCharacterToAim").boolValue = true;
        aimSo.FindProperty("rotateWeaponPivotToAim").boolValue = true;
        aimSo.FindProperty("characterTurnSpeed").floatValue = 720f;
        aimSo.FindProperty("weaponTurnSpeed").floatValue = 1080f;
        aimSo.FindProperty("drawDebugAimRay").boolValue = true;
        aimSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(input);
        EditorUtility.SetDirty(toolSwitcher);
        EditorUtility.SetDirty(visualBinder);
        return player;
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
    }

    private static GameObject CreateSwimFormVisual(Transform parent, Material material)
    {
        GameObject swimFormVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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

        MeshRenderer renderer = swimFormVisual.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        swimFormVisual.SetActive(false);
        return swimFormVisual;
    }

    private static void ConfigureCameraFollow(Camera camera, Transform target)
    {
        if (camera == null || target == null)
        {
            return;
        }

        ThirdPersonCameraFollow follow = camera.gameObject.AddComponent<ThirdPersonCameraFollow>();
        SerializedObject followSo = new SerializedObject(follow);
        followSo.FindProperty("target").objectReferenceValue = target;
        followSo.FindProperty("followOffset").vector3Value = new Vector3(0f, 4.5f, -6f);
        followSo.FindProperty("useTargetRotation").boolValue = true;
        followSo.FindProperty("positionSmoothSpeed").floatValue = 10f;
        followSo.FindProperty("rotationSmoothSpeed").floatValue = 12f;
        followSo.FindProperty("lookAtHeight").floatValue = 1.25f;
        followSo.FindProperty("lookAheadDistance").floatValue = 1.5f;
        followSo.FindProperty("lookSideOffset").floatValue = 0.25f;
        followSo.FindProperty("enableMouseOrbit").boolValue = true;
        followSo.FindProperty("lockCursorOnPlay").boolValue = true;
        followSo.FindProperty("yawSensitivity").floatValue = 180f;
        followSo.FindProperty("pitchSensitivity").floatValue = 120f;
        followSo.FindProperty("shoulderOffset").floatValue = 0.75f;
        followSo.FindProperty("orbitDistance").floatValue = 6.5f;
        followSo.FindProperty("initialPitch").floatValue = 18f;
        followSo.FindProperty("minPitch").floatValue = -20f;
        followSo.FindProperty("maxPitch").floatValue = 65f;
        followSo.ApplyModifiedPropertiesWithoutUndo();

        camera.transform.position = target.TransformPoint(new Vector3(0f, 4.5f, -6f));
        camera.transform.rotation = Quaternion.LookRotation(
            (target.position + Vector3.up * 1.25f) - camera.transform.position,
            Vector3.up);
    }
}
