using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only utility that creates the standalone main menu scene with the authored menu prefab.
/// </summary>
public static class SplatFightersMenuSceneSetup
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string MainMenuCanvasPrefabPath = "Assets/Resources/UI/MainMenu/Prefabs/MainMenuCanvas.prefab";

    [MenuItem("Tools/Splat Fighters/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        CreateMenuCamera();

        GameObject controllerObject = new GameObject("MainMenuController");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();
        GameObject menuCanvas = CreateMenuCanvas();
        CreateEventSystem();
        AssignMenuReferences(controller, menuCanvas);

        EditorSceneManager.SaveScene(scene, MenuScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true),
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created standalone main menu scene at {MenuScenePath} and updated build settings.");
    }

    private static GameObject CreateMenuCanvas()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning($"Main menu prefab is missing at {MainMenuCanvasPrefabPath}. The controller will load it from Resources at runtime if available.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "MainMenuCanvas";
        return instance;
    }

    private static void CreateMenuCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void AssignMenuReferences(MainMenuController controller, GameObject menuCanvas)
    {
        if (controller == null)
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuCanvasPrefabPath);
        MainMenuView prefabView = prefab != null ? prefab.GetComponent<MainMenuView>() : null;
        MainMenuView sceneView = menuCanvas != null ? menuCanvas.GetComponent<MainMenuView>() : null;

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("menuViewPrefab").objectReferenceValue = prefabView;
        serializedController.FindProperty("menuView").objectReferenceValue = sceneView;
        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
