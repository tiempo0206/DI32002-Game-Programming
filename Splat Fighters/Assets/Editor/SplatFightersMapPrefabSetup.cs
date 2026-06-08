using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves the authored arena hierarchy as a reusable map prefab and reconnects scene references.
/// </summary>
public static class SplatFightersMapPrefabSetup
{
    private const string GameplayScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string MapPrefabFolder = "Assets/Prefabs/Maps";
    private const string MapPrefabPath = MapPrefabFolder + "/HangarArenaMap.prefab";
    private const string LevelRootName = "LevelRoot";
    private const string PaintableGroundName = "PaintableGround";

    [MenuItem("Tools/Splat Fighters/Apply Map Prefab Architecture")]
    public static void ApplyMapPrefabArchitecture()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
        ApplyGameplayMapPrefabInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Applied Splat Fighters map prefab architecture at {MapPrefabPath}.");
    }

    public static void ApplyGameplayMapPrefabInCurrentScene()
    {
        SplatFightersActorPrefabSetup.ApplyGameplayActorPrefabsInCurrentScene();
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Maps");

        GameObject levelRoot = GameObject.Find(LevelRootName);

        if (levelRoot == null)
        {
            Debug.LogError($"Cannot create map prefab because {LevelRootName} was not found in the active scene.");
            return;
        }

        AttachPaintableGround(levelRoot.transform);
        PrefabUtility.SaveAsPrefabAssetAndConnect(levelRoot, MapPrefabPath, InteractionMode.AutomatedAction);
        RebindSceneReferences();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static void AttachPaintableGround(Transform levelRoot)
    {
        GameObject paintableGround = GameObject.Find(PaintableGroundName);

        if (paintableGround == null || paintableGround.transform == levelRoot || paintableGround.transform.IsChildOf(levelRoot))
        {
            return;
        }

        paintableGround.transform.SetParent(levelRoot, true);
        EditorUtility.SetDirty(paintableGround);
        EditorUtility.SetDirty(levelRoot);
    }

    private static void RebindSceneReferences()
    {
        PaintManager paintManager = Object.FindObjectOfType<PaintManager>();
        PaintableArea paintableArea = Object.FindObjectOfType<PaintableArea>();
        GameManager gameManager = Object.FindObjectOfType<GameManager>();
        GameObject player = GameObject.Find("Player");
        BotController bot = Object.FindObjectOfType<BotController>();
        TowerObjective centerTower = Object.FindObjectOfType<TowerObjective>();
        SpawnPoint teamASpawn = FindDefaultSpawnPoint(Team.TeamA);
        SpawnPoint teamBSpawn = FindDefaultSpawnPoint(Team.TeamB);

        if (paintManager != null)
        {
            SerializedObject paintManagerSo = new SerializedObject(paintManager);
            paintManagerSo.FindProperty("autoFindAreasOnAwake").boolValue = true;

            SerializedProperty areas = paintManagerSo.FindProperty("paintableAreas");
            areas.ClearArray();

            if (paintableArea != null)
            {
                areas.InsertArrayElementAtIndex(0);
                areas.GetArrayElementAtIndex(0).objectReferenceValue = paintableArea;
            }

            paintManagerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(paintManager);
        }

        if (gameManager == null)
        {
            return;
        }

        SerializedObject managerSo = new SerializedObject(gameManager);
        managerSo.FindProperty("paintManager").objectReferenceValue = paintManager;
        managerSo.FindProperty("playerRoot").objectReferenceValue = player != null ? player.transform : null;
        managerSo.FindProperty("playerController").objectReferenceValue = player != null ? player.GetComponent<PlayerController>() : null;
        managerSo.FindProperty("playerHealth").objectReferenceValue = player != null ? player.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("playerWeapon").objectReferenceValue = player != null ? player.GetComponentInChildren<InkWeapon>() : null;
        managerSo.FindProperty("playerToolSwitcher").objectReferenceValue = player != null ? player.GetComponent<PlayerToolSwitcher>() : null;
        managerSo.FindProperty("playerSpecialMeter").objectReferenceValue = player != null ? player.GetComponentInChildren<SpecialMeter>() : null;
        managerSo.FindProperty("centerTowerObjective").objectReferenceValue = centerTower;
        managerSo.FindProperty("teamBBot").objectReferenceValue = bot;
        managerSo.FindProperty("teamBBotHealth").objectReferenceValue = bot != null ? bot.GetComponent<CharacterHealth>() : null;
        managerSo.FindProperty("teamASpawn").objectReferenceValue = teamASpawn;
        managerSo.FindProperty("teamBSpawn").objectReferenceValue = teamBSpawn;
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

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
