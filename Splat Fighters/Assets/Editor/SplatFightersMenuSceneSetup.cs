using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only utility that creates the standalone main menu scene and wires it into build settings.
/// </summary>
public static class SplatFightersMenuSceneSetup
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/MVP_ShootingTest.unity";

    [MenuItem("Tools/Splat Fighters/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        GameObject controllerObject = new GameObject("MainMenuController");
        controllerObject.AddComponent<MainMenuController>();

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

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
