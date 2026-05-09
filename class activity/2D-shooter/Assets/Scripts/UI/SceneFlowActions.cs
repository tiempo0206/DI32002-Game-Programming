using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles retry and return-to-menu flows from in-game UI buttons.
/// </summary>
public class SceneFlowActions : MonoBehaviour
{
    [Tooltip("The scene to reload for retry and return-to-menu actions.")]
    [SerializeField] private string sceneName = "Level1_scene";

    public void RetryLevel()
    {
        StartMenuController.startImmediatelyOnNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToMenu()
    {
        StartMenuController.startImmediatelyOnNextLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
