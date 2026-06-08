using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates reusable actor prefabs for Splat Fighters and reconnects scene actors to them.
/// </summary>
public static class SplatFightersActorPrefabSetup
{
    private const string GameplayScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string TrainingScenePath = "Assets/Scenes/HowToPlayTraining.unity";
    private const string CharacterPrefabFolder = "Assets/Prefabs/Characters";
    private const string PlayerActorPrefabPath = CharacterPrefabFolder + "/PlayerActor.prefab";
    private const string BotActorPrefabPath = CharacterPrefabFolder + "/BotActor.prefab";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const string TeamAPlayerMaterialPath = "Assets/Materials/Teams/MAT_TeamA_Player.mat";
    private const string TeamBBotMaterialPath = "Assets/Materials/Teams/MAT_TeamB_Bot.mat";
    private const string ActorsRootName = "Actors";

    private const float CharacterRootHeight = 0.8f;
    private const float CharacterControllerHeight = 1.6f;
    private const float CharacterControllerRadius = 0.4f;

    [MenuItem("Tools/Splat Fighters/Apply Actor Prefab Architecture")]
    public static void ApplyActorPrefabArchitecture()
    {
        EnsureActorPrefabs();
        ConvertGameplayScene();
        ConvertTrainingScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Splat Fighters actor prefab architecture.");
    }

    public static void ApplyGameplayActorPrefabsInCurrentScene()
    {
        EnsureActorPrefabs();
        ConvertGameplaySceneInCurrentScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    public static void ApplyTrainingActorPrefabsInCurrentScene()
    {
        EnsureActorPrefabs();
        ConvertTrainingSceneInCurrentScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Splat Fighters/Rebuild Actor Prefabs")]
    public static void EnsureActorPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Characters");

        Material teamAMaterial = LoadMaterial(TeamAPlayerMaterialPath, TeamVisualPalette.TeamAColor);
        Material teamBMaterial = LoadMaterial(TeamBBotMaterialPath, TeamVisualPalette.TeamBColor);
        InkProjectile projectilePrefab = AssetDatabase.LoadAssetAtPath<InkProjectile>(ProjectilePrefabPath);

        GameObject playerActor = CreatePlayerActorTemplate(teamAMaterial, projectilePrefab);
        PrefabUtility.SaveAsPrefabAsset(playerActor, PlayerActorPrefabPath);
        Object.DestroyImmediate(playerActor);

        GameObject botActor = CreateBotActorTemplate(teamBMaterial, projectilePrefab);
        PrefabUtility.SaveAsPrefabAsset(botActor, BotActorPrefabPath);
        Object.DestroyImmediate(botActor);

        AssetDatabase.SaveAssets();
    }

    private static void ConvertGameplayScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        ConvertGameplaySceneInCurrentScene(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConvertGameplaySceneInCurrentScene(Scene scene)
    {
        Transform actorsRoot = FindOrCreateSceneRoot(ActorsRootName);
        GameObject player = EnsureSceneActor("Player", PlayerActorPrefabPath, actorsRoot, new Vector3(0f, CharacterRootHeight, -15.2f), Quaternion.identity);
        GameObject bot = EnsureSceneActor("TeamBBot", BotActorPrefabPath, actorsRoot, new Vector3(0f, CharacterRootHeight, 14.15f), Quaternion.LookRotation(Vector3.back, Vector3.up));

        ConfigurePlayerSceneOverrides(player, false);
        ConfigureBotSceneOverrides(bot);
        RebindCamera(player);
        RebindGameManager();

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ConvertTrainingScene()
    {
        Scene scene = EditorSceneManager.OpenScene(TrainingScenePath, OpenSceneMode.Single);
        ConvertTrainingSceneInCurrentScene(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConvertTrainingSceneInCurrentScene(Scene scene)
    {
        Transform actorsRoot = FindOrCreateSceneRoot(ActorsRootName);
        GameObject player = EnsureSceneActor("Player", PlayerActorPrefabPath, actorsRoot, new Vector3(0f, CharacterRootHeight, -6.2f), Quaternion.identity);

        ConfigurePlayerSceneOverrides(player, true);
        RebindCamera(player);
        RebindGameManager();
        RebindTrainingLesson(player);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject EnsureSceneActor(string sceneObjectName, string prefabPath, Transform fallbackParent, Vector3 fallbackPosition, Quaternion fallbackRotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject existing = GameObject.Find(sceneObjectName);

        if (prefab == null)
        {
            Debug.LogError($"Missing actor prefab at {prefabPath}.");
            return existing;
        }

        bool isMatchingPrefab = existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing) == prefab;

        if (isMatchingPrefab)
        {
            if (fallbackParent != null && existing.transform.parent != fallbackParent)
            {
                existing.transform.SetParent(fallbackParent, true);
                EditorUtility.SetDirty(existing);
            }

            EnsureActorRequiredComponents(existing);
            return existing;
        }

        Transform parent = fallbackParent != null ? fallbackParent : existing != null ? existing.transform.parent : null;
        Vector3 position = existing != null ? existing.transform.position : fallbackPosition;
        Quaternion rotation = existing != null ? existing.transform.rotation : fallbackRotation;
        Vector3 localScale = existing != null ? existing.transform.localScale : Vector3.one;
        int siblingIndex = existing != null ? existing.transform.GetSiblingIndex() : -1;

        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject actor = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        actor.name = sceneObjectName;
        actor.transform.SetPositionAndRotation(position, rotation);
        actor.transform.localScale = localScale;

        if (siblingIndex >= 0)
        {
            actor.transform.SetSiblingIndex(siblingIndex);
        }

        EnsureActorRequiredComponents(actor);
        return actor;
    }

    private static Transform FindOrCreateSceneRoot(string name)
    {
        GameObject existing = GameObject.Find(name);

        if (existing == null)
        {
            existing = new GameObject(name);
        }

        if (existing.transform.parent != null)
        {
            existing.transform.SetParent(null, true);
        }

        EditorUtility.SetDirty(existing);
        return existing.transform;
    }

    private static GameObject CreatePlayerActorTemplate(Material material, InkProjectile projectilePrefab)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "PlayerActor";
        DestroyCollider(player);
        AssignMaterial(player, material);
        ConfigureCharacterController(player.AddComponent<CharacterController>());

        GameObject swimFormVisual = CreateSwimFormVisual(player.transform, material);
        Transform firePoint = CreateFirePoint(player.transform, "FirePoint", new Vector3(0f, 0.35f, 0.72f), Quaternion.identity);
        RollerPaintTool rollerPaintTool = CreateRollerTool(player.transform, material);

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        PlayerController playerController = player.AddComponent<PlayerController>();
        CharacterHealth health = player.AddComponent<CharacterHealth>();
        InkWeapon weapon = player.AddComponent<InkWeapon>();
        SpecialMeter specialMeter = player.AddComponent<SpecialMeter>();
        SpecialPaintBurst specialPaintBurst = player.AddComponent<SpecialPaintBurst>();
        PlayerToolSwitcher toolSwitcher = player.AddComponent<PlayerToolSwitcher>();
        AimController aimController = player.AddComponent<AimController>();
        TeamVisualBinder visualBinder = player.AddComponent<TeamVisualBinder>();
        player.AddComponent<CharacterVisualController>();

        ConfigurePlayerInput(input);
        ConfigurePlayerController(playerController, null, weapon, aimController, toolSwitcher, swimFormVisual, player.GetComponent<Renderer>(), true);
        ConfigureHealth(health, Team.TeamA, 35f, player.transform);
        ConfigurePlayerWeapon(weapon, projectilePrefab, firePoint, player.transform, false);
        ConfigureSpecialMeter(specialMeter, 180);
        ConfigureSpecialPaintBurst(specialPaintBurst, specialMeter, aimController, 4.25f);
        ConfigureRollerPaintTool(rollerPaintTool, player.transform, 1.05f);
        ConfigureToolSwitcher(toolSwitcher, weapon, rollerPaintTool);
        ConfigureAimController(aimController, null, weapon, firePoint, player.transform);
        visualBinder.Configure(Team.TeamA, material, null);

        return player;
    }

    private static GameObject CreateBotActorTemplate(Material material, InkProjectile projectilePrefab)
    {
        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bot.name = "BotActor";
        DestroyCollider(bot);
        AssignMaterial(bot, material);
        CharacterController characterController = bot.AddComponent<CharacterController>();
        ConfigureCharacterController(characterController);

        Transform firePoint = CreateFirePoint(bot.transform, "TeamBBotFirePoint", new Vector3(0f, 0.35f, 0.7f), Quaternion.identity);
        TeamVisualBinder visualBinder = bot.AddComponent<TeamVisualBinder>();
        bot.AddComponent<CharacterVisualController>();
        CharacterHealth health = bot.AddComponent<CharacterHealth>();
        InkWeapon weapon = bot.AddComponent<InkWeapon>();
        BotController botController = bot.AddComponent<BotController>();

        visualBinder.Configure(Team.TeamB, null, material);
        ConfigureHealth(health, Team.TeamB, 35f, bot.transform);
        ConfigureBotWeapon(weapon, projectilePrefab, firePoint, bot.transform);
        ConfigureBotController(botController, characterController, weapon, health, firePoint, null, new Transform[0], new Transform[0]);

        return bot;
    }

    private static void ConfigurePlayerSceneOverrides(GameObject player, bool trainingScene)
    {
        if (player == null)
        {
            return;
        }

        Material material = LoadMaterial(TeamAPlayerMaterialPath, TeamVisualPalette.TeamAColor);
        Camera camera = Camera.main;
        InkProjectile projectilePrefab = AssetDatabase.LoadAssetAtPath<InkProjectile>(ProjectilePrefabPath);
        Renderer renderer = player.GetComponent<Renderer>();
        PlayerInputHandler input = player.GetComponent<PlayerInputHandler>();
        PlayerController controller = player.GetComponent<PlayerController>();
        CharacterHealth health = player.GetComponent<CharacterHealth>();
        InkWeapon weapon = player.GetComponentInChildren<InkWeapon>();
        SpecialMeter specialMeter = player.GetComponentInChildren<SpecialMeter>();
        SpecialPaintBurst specialPaintBurst = player.GetComponent<SpecialPaintBurst>();
        PlayerToolSwitcher toolSwitcher = player.GetComponent<PlayerToolSwitcher>();
        AimController aimController = player.GetComponent<AimController>();
        TeamVisualBinder visualBinder = player.GetComponent<TeamVisualBinder>();
        RollerPaintTool rollerPaintTool = player.GetComponentInChildren<RollerPaintTool>(true);
        Transform firePoint = player.transform.Find("FirePoint");
        GameObject swimFormVisual = EnsureSwimFormVisual(player.transform, material);

        AssignMaterial(player, material);
        ConfigureCharacterController(player.GetComponent<CharacterController>());
        ConfigurePlayerInput(input);
        ConfigurePlayerController(controller, camera != null ? camera.transform : null, weapon, aimController, toolSwitcher, swimFormVisual, renderer, !trainingScene);
        ConfigureHealth(health, Team.TeamA, trainingScene ? 0f : 35f, player.transform);
        ConfigurePlayerWeapon(weapon, projectilePrefab, firePoint, player.transform, trainingScene);
        ConfigureSpecialMeter(specialMeter, trainingScene ? 70 : 180);
        ConfigureSpecialPaintBurst(specialPaintBurst, specialMeter, aimController, trainingScene ? 3.2f : 4.25f);
        ConfigureRollerPaintTool(rollerPaintTool, player.transform, trainingScene ? 0.95f : 1.05f);
        ConfigureToolSwitcher(toolSwitcher, weapon, rollerPaintTool);
        ConfigureAimController(aimController, camera, weapon, firePoint, player.transform);

        if (visualBinder != null)
        {
            visualBinder.Configure(Team.TeamA, material, null);
        }

        EnsureActorRequiredComponents(player);
        EditorUtility.SetDirty(player);
    }

    private static void ConfigureBotSceneOverrides(GameObject bot)
    {
        if (bot == null)
        {
            return;
        }

        Material material = LoadMaterial(TeamBBotMaterialPath, TeamVisualPalette.TeamBColor);
        InkProjectile projectilePrefab = AssetDatabase.LoadAssetAtPath<InkProjectile>(ProjectilePrefabPath);
        Transform firePoint = bot.transform.Find("TeamBBotFirePoint");
        CharacterController characterController = bot.GetComponent<CharacterController>();
        CharacterHealth health = bot.GetComponent<CharacterHealth>();
        InkWeapon weapon = bot.GetComponentInChildren<InkWeapon>();
        BotController botController = bot.GetComponent<BotController>();
        TeamVisualBinder visualBinder = bot.GetComponent<TeamVisualBinder>();

        AssignMaterial(bot, material);
        ConfigureCharacterController(characterController);
        ConfigureHealth(health, Team.TeamB, 35f, bot.transform);
        ConfigureBotWeapon(weapon, projectilePrefab, firePoint, bot.transform);

        Transform[] waypoints =
        {
            FindTransform("TeamBBotPatrol_01"),
            FindTransform("TeamBBotPatrol_02"),
            FindTransform("TeamBBotPatrol_03"),
            FindTransform("TeamBBotPatrol_04"),
            FindTransform("TeamBBotPatrol_05"),
            FindTransform("TeamBBotPatrol_06")
        };
        Transform[] paintTargets =
        {
            FindTransform("TeamBBotPaintTarget_Center"),
            FindTransform("TeamBBotPaintTarget_LeftLane"),
            FindTransform("TeamBBotPaintTarget_RightLane"),
            FindTransform("TeamBBotPaintTarget_TeamASide"),
            FindTransform("TeamBBotPaintTarget_TeamAFlank"),
            FindTransform("TeamBBotPaintTarget_TeamAObjective")
        };

        ConfigureBotController(botController, characterController, weapon, health, firePoint, FindTransform("TeamBSpawn"), waypoints, paintTargets);

        if (visualBinder != null)
        {
            visualBinder.Configure(Team.TeamB, null, material);
        }

        EnsureActorRequiredComponents(bot);
        EditorUtility.SetDirty(bot);
    }

    private static void RebindCamera(GameObject player)
    {
        if (player == null || Camera.main == null)
        {
            return;
        }

        ThirdPersonCameraFollow follow = Camera.main.GetComponent<ThirdPersonCameraFollow>();

        if (follow == null)
        {
            follow = Camera.main.gameObject.AddComponent<ThirdPersonCameraFollow>();
        }

        SerializedObject followSo = new SerializedObject(follow);
        followSo.FindProperty("target").objectReferenceValue = player.transform;
        followSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(follow);
    }

    private static void RebindGameManager()
    {
        GameManager gameManager = Object.FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        BotController bot = Object.FindObjectOfType<BotController>();
        PaintManager paintManager = Object.FindObjectOfType<PaintManager>();
        TowerObjective towerObjective = Object.FindObjectOfType<TowerObjective>();

        SerializedObject managerSo = new SerializedObject(gameManager);
        managerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        managerSo.FindProperty("playerRoot").objectReferenceValue = player != null ? player.transform : null;
        managerSo.FindProperty("playerController").objectReferenceValue = player != null ? player.GetComponent<PlayerController>() : null;
        managerSo.FindProperty("playerHealth").objectReferenceValue = player != null ? player.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("playerWeapon").objectReferenceValue = player != null ? player.GetComponentInChildren<InkWeapon>() : null;
        managerSo.FindProperty("playerToolSwitcher").objectReferenceValue = player != null ? player.GetComponent<PlayerToolSwitcher>() : null;
        managerSo.FindProperty("playerSpecialMeter").objectReferenceValue = player != null ? player.GetComponentInChildren<SpecialMeter>() : null;
        managerSo.FindProperty("centerTowerObjective").objectReferenceValue = towerObjective;
        managerSo.FindProperty("teamBBot").objectReferenceValue = bot;
        managerSo.FindProperty("teamBBotHealth").objectReferenceValue = bot != null ? bot.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("teamASpawn").objectReferenceValue = FindDefaultSpawnPoint(Team.TeamA);
        managerSo.FindProperty("teamBSpawn").objectReferenceValue = FindDefaultSpawnPoint(Team.TeamB);
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(gameManager);
    }

    private static void RebindTrainingLesson(GameObject player)
    {
        TrainingLessonController lesson = Object.FindObjectOfType<TrainingLessonController>();

        if (lesson == null || player == null)
        {
            return;
        }

        SerializedObject lessonSo = new SerializedObject(lesson);
        lessonSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
        lessonSo.FindProperty("playerTransform").objectReferenceValue = player.transform;
        lessonSo.FindProperty("paintManager").objectReferenceValue = Object.FindObjectOfType<PaintManager>();
        lessonSo.FindProperty("specialMeter").objectReferenceValue = player.GetComponentInChildren<SpecialMeter>();
        lessonSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(lesson);
    }

    private static void ConfigurePlayerInput(PlayerInputHandler input)
    {
        if (input == null)
        {
            return;
        }

        SerializedObject inputSo = new SerializedObject(input);
        inputSo.FindProperty("swimKey").intValue = (int)KeyCode.LeftShift;
        inputSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(input);
    }

    private static void ConfigurePlayerController(PlayerController controller, Transform cameraTransform, InkWeapon weapon, AimController aimController, PlayerToolSwitcher toolSwitcher, GameObject swimFormVisual, Renderer humanoidRenderer, bool enablePaintRoutes)
    {
        if (controller == null)
        {
            return;
        }

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("cameraTransform").objectReferenceValue = cameraTransform;
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
        controllerSo.FindProperty("groundProbe").objectReferenceValue = controller.transform;
        controllerSo.FindProperty("swimFormVisual").objectReferenceValue = swimFormVisual;
        controllerSo.FindProperty("enablePaintRoutes").boolValue = enablePaintRoutes;
        controllerSo.FindProperty("paintRouteProbeRadius").floatValue = 0.75f;
        controllerSo.FindProperty("paintRouteProbeOffset").vector3Value = new Vector3(0f, 0.45f, 0f);
        controllerSo.FindProperty("enableJump").boolValue = true;
        controllerSo.FindProperty("jumpHeight").floatValue = 1.2f;

        SerializedProperty renderers = controllerSo.FindProperty("humanoidRenderers");
        renderers.ClearArray();
        if (humanoidRenderer != null)
        {
            renderers.InsertArrayElementAtIndex(0);
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = humanoidRenderer;
        }

        controllerSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureHealth(CharacterHealth health, Team team, float enemyPaintDamagePerSecond, Transform groundProbe)
    {
        if (health == null)
        {
            return;
        }

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("team").enumValueIndex = (int)team;
        healthSo.FindProperty("maxHealth").floatValue = 100f;
        healthSo.FindProperty("enemyPaintDamagePerSecond").floatValue = enemyPaintDamagePerSecond;
        healthSo.FindProperty("damageOnlyDuringMatch").boolValue = true;
        healthSo.FindProperty("groundProbe").objectReferenceValue = groundProbe;
        healthSo.FindProperty("hideRenderersWhileEliminated").boolValue = true;
        healthSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(health);
    }

    private static void ConfigurePlayerWeapon(InkWeapon weapon, InkProjectile projectilePrefab, Transform firePoint, Transform groundProbe, bool trainingScene)
    {
        if (weapon == null)
        {
            return;
        }

        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePoint;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = trainingScene ? 1.65f : 1.75f;
        weaponSo.FindProperty("fireCooldown").floatValue = trainingScene ? 0.18f : 0.2f;
        weaponSo.FindProperty("useInkResource").boolValue = true;
        weaponSo.FindProperty("maxInk").floatValue = 100f;
        weaponSo.FindProperty("inkPerShot").floatValue = trainingScene ? 8f : 10f;
        weaponSo.FindProperty("inkRecoveryPerSecond").floatValue = trainingScene ? 15f : 12f;
        weaponSo.FindProperty("ownPaintRecoveryMultiplier").floatValue = trainingScene ? 4f : 3.5f;
        weaponSo.FindProperty("startWithFullInk").boolValue = true;
        weaponSo.FindProperty("groundProbe").objectReferenceValue = groundProbe;
        weaponSo.FindProperty("useCameraAim").boolValue = false;
        weaponSo.FindProperty("paintDirectlyAtAimTarget").boolValue = true;
        weaponSo.FindProperty("projectileIsVisualOnlyWhenDirectPainting").boolValue = true;
        weaponSo.FindProperty("applyTeamColorToProjectile").boolValue = true;
        weaponSo.FindProperty("teamAProjectileColor").colorValue = TeamVisualPalette.TeamAColor;
        weaponSo.FindProperty("teamBProjectileColor").colorValue = TeamVisualPalette.TeamBColor;
        weaponSo.FindProperty("enableKeyboardTestFire").boolValue = false;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void ConfigureBotWeapon(InkWeapon weapon, InkProjectile projectilePrefab, Transform firePoint, Transform groundProbe)
    {
        if (weapon == null)
        {
            return;
        }

        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePoint;
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
        weaponSo.FindProperty("groundProbe").objectReferenceValue = groundProbe;
        weaponSo.FindProperty("useCameraAim").boolValue = false;
        weaponSo.FindProperty("paintDirectlyAtAimTarget").boolValue = true;
        weaponSo.FindProperty("projectileIsVisualOnlyWhenDirectPainting").boolValue = true;
        weaponSo.FindProperty("applyTeamColorToProjectile").boolValue = true;
        weaponSo.FindProperty("teamAProjectileColor").colorValue = TeamVisualPalette.TeamAColor;
        weaponSo.FindProperty("teamBProjectileColor").colorValue = TeamVisualPalette.TeamBColor;
        weaponSo.FindProperty("enableKeyboardTestFire").boolValue = false;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(weapon);
    }

    private static void ConfigureSpecialMeter(SpecialMeter specialMeter, int changedCellsForFullCharge)
    {
        if (specialMeter == null)
        {
            return;
        }

        SerializedObject specialSo = new SerializedObject(specialMeter);
        specialSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        specialSo.FindProperty("changedCellsForFullCharge").intValue = changedCellsForFullCharge;
        specialSo.FindProperty("startingChargePercent").floatValue = 0f;
        specialSo.FindProperty("resetWhenPaintCleared").boolValue = true;
        specialSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(specialMeter);
    }

    private static void ConfigureSpecialPaintBurst(SpecialPaintBurst specialPaintBurst, SpecialMeter specialMeter, AimController aimController, float burstRadius)
    {
        if (specialPaintBurst == null)
        {
            return;
        }

        SerializedObject burstSo = new SerializedObject(specialPaintBurst);
        burstSo.FindProperty("specialMeter").objectReferenceValue = specialMeter;
        burstSo.FindProperty("aimController").objectReferenceValue = aimController;
        burstSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        burstSo.FindProperty("burstPaintRadius").floatValue = burstRadius;
        burstSo.FindProperty("fallbackDistance").floatValue = 4.5f;
        burstSo.FindProperty("activationKey").intValue = (int)KeyCode.Q;
        burstSo.FindProperty("requireMatchPlaying").boolValue = true;
        burstSo.FindProperty("spawnInkSplatterVfx").boolValue = true;
        burstSo.FindProperty("splatterRadiusMultiplier").floatValue = 1.15f;
        burstSo.FindProperty("logActivation").boolValue = false;
        burstSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(specialPaintBurst);
    }

    private static void ConfigureRollerPaintTool(RollerPaintTool rollerPaintTool, Transform paintOrigin, float paintRadius)
    {
        if (rollerPaintTool == null)
        {
            return;
        }

        SerializedObject rollerSo = new SerializedObject(rollerPaintTool);
        rollerSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        rollerSo.FindProperty("paintKey").intValue = (int)KeyCode.Mouse0;
        rollerSo.FindProperty("requireInput").boolValue = true;
        rollerSo.FindProperty("requireMatchPlaying").boolValue = true;
        rollerSo.FindProperty("paintOrigin").objectReferenceValue = paintOrigin;
        rollerSo.FindProperty("paintInterval").floatValue = 0.08f;
        rollerSo.FindProperty("paintRadius").floatValue = paintRadius;
        rollerSo.FindProperty("forwardOffset").floatValue = paintRadius > 1f ? 1.15f : 1.1f;
        rollerSo.FindProperty("halfWidth").floatValue = paintRadius > 1f ? 0.65f : 0.6f;
        rollerSo.FindProperty("swathSamples").intValue = 3;
        rollerSo.FindProperty("fallbackPaintPlaneY").floatValue = 0f;
        rollerSo.FindProperty("groundProbeLayers").intValue = ~0;
        rollerSo.FindProperty("requireMovementForTrail").boolValue = true;
        rollerSo.FindProperty("minMoveDistance").floatValue = 0.06f;
        rollerSo.ApplyModifiedPropertiesWithoutUndo();
        rollerPaintTool.enabled = false;
        EditorUtility.SetDirty(rollerPaintTool);
    }

    private static void ConfigureToolSwitcher(PlayerToolSwitcher toolSwitcher, InkWeapon weapon, RollerPaintTool rollerPaintTool)
    {
        if (toolSwitcher == null)
        {
            return;
        }

        SerializedObject toolSo = new SerializedObject(toolSwitcher);
        toolSo.FindProperty("defaultTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("currentTool").enumValueIndex = (int)PlayerToolSwitcher.ToolMode.Shooter;
        toolSo.FindProperty("shooter").objectReferenceValue = weapon;
        toolSo.FindProperty("roller").objectReferenceValue = rollerPaintTool;
        toolSo.FindProperty("enableKeyboardSwitching").boolValue = true;
        toolSo.FindProperty("shooterKey").intValue = (int)KeyCode.Alpha1;
        toolSo.FindProperty("rollerKey").intValue = (int)KeyCode.Alpha2;

        SerializedProperty rollerRenderers = toolSo.FindProperty("rollerRenderers");
        rollerRenderers.ClearArray();

        if (rollerPaintTool != null)
        {
            Renderer[] renderers = rollerPaintTool.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                rollerRenderers.InsertArrayElementAtIndex(i);
                rollerRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                renderers[i].enabled = false;
                EditorUtility.SetDirty(renderers[i]);
            }
        }

        toolSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(toolSwitcher);
    }

    private static void ConfigureAimController(AimController aimController, Camera camera, InkWeapon weapon, Transform firePoint, Transform characterRoot)
    {
        if (aimController == null)
        {
            return;
        }

        SerializedObject aimSo = new SerializedObject(aimController);
        aimSo.FindProperty("aimCamera").objectReferenceValue = camera;
        aimSo.FindProperty("weapon").objectReferenceValue = weapon;
        aimSo.FindProperty("firePoint").objectReferenceValue = firePoint;
        aimSo.FindProperty("characterRoot").objectReferenceValue = characterRoot;
        aimSo.FindProperty("weaponPivot").objectReferenceValue = firePoint;
        aimSo.FindProperty("ignoredRoot").objectReferenceValue = characterRoot;
        aimSo.FindProperty("autoCreateReticle").boolValue = true;
        aimSo.FindProperty("aimInputMode").enumValueIndex = 0;
        aimSo.FindProperty("maxAimDistance").floatValue = 100f;
        aimSo.FindProperty("rotateCharacterToAim").boolValue = true;
        aimSo.FindProperty("rotateWeaponPivotToAim").boolValue = true;
        aimSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(aimController);
    }

    private static void ConfigureBotController(BotController botController, CharacterController characterController, InkWeapon weapon, CharacterHealth health, Transform firePoint, Transform retreatTarget, Transform[] waypoints, Transform[] paintTargets)
    {
        if (botController == null)
        {
            return;
        }

        SerializedObject botSo = new SerializedObject(botController);
        botSo.FindProperty("characterController").objectReferenceValue = characterController;
        botSo.FindProperty("weapon").objectReferenceValue = weapon;
        botSo.FindProperty("health").objectReferenceValue = health;
        botSo.FindProperty("firePoint").objectReferenceValue = firePoint;
        botSo.FindProperty("botTeam").enumValueIndex = (int)Team.TeamB;
        botSo.FindProperty("priorityPaintTargetTeam").enumValueIndex = (int)Team.TeamA;
        AssignTransformArray(botSo.FindProperty("waypoints"), waypoints);
        botSo.FindProperty("retreatTarget").objectReferenceValue = retreatTarget;
        botSo.FindProperty("patrolOnStart").boolValue = true;
        botSo.FindProperty("fireOnStart").boolValue = true;
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
        EditorUtility.SetDirty(botController);
    }

    private static void EnsureActorRequiredComponents(GameObject actor)
    {
        if (actor == null)
        {
            return;
        }

        if (actor.GetComponent<CharacterVisualController>() == null)
        {
            actor.AddComponent<CharacterVisualController>();
        }

        EditorUtility.SetDirty(actor);
    }

    private static GameObject EnsureSwimFormVisual(Transform parent, Material material)
    {
        Transform existing = parent.Find("SwimFormVisual");
        return existing != null ? existing.gameObject : CreateSwimFormVisual(parent, material);
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

    private static Transform CreateFirePoint(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject firePoint = new GameObject(name);
        firePoint.transform.SetParent(parent, false);
        firePoint.transform.localPosition = localPosition;
        firePoint.transform.localRotation = localRotation;
        return firePoint.transform;
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

    private static void ConfigureCharacterController(CharacterController controller)
    {
        if (controller == null)
        {
            return;
        }

        controller.height = CharacterControllerHeight;
        controller.radius = CharacterControllerRadius;
        controller.center = Vector3.zero;
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.24f;
        EditorUtility.SetDirty(controller);
    }

    private static void AssignTransformArray(SerializedProperty arrayProperty, Transform[] values)
    {
        arrayProperty.ClearArray();

        if (values == null)
        {
            return;
        }

        int writeIndex = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
            {
                continue;
            }

            arrayProperty.InsertArrayElementAtIndex(writeIndex);
            arrayProperty.GetArrayElementAtIndex(writeIndex).objectReferenceValue = values[i];
            writeIndex++;
        }
    }

    private static Transform FindTransform(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null ? found.transform : null;
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

    private static Material LoadMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AssignMaterial(GameObject target, Material material)
    {
        Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void DestroyCollider(GameObject target)
    {
        Collider collider = target != null ? target.GetComponent<Collider>() : null;

        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
