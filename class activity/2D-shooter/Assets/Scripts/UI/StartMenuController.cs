using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the start menu overlay and gates gameplay until the player starts.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Tooltip("The menu root to hide once gameplay starts.")]
    [SerializeField] private GameObject menuRoot = null;

    [Tooltip("The button that starts the game.")]
    [SerializeField] private Button startButton = null;

    [Tooltip("Gameplay behaviours to keep disabled while the start menu is open.")]
    [SerializeField] private MonoBehaviour[] gameplayBehaviours = new MonoBehaviour[0];

    private void Awake()
    {
        if (menuRoot == null)
        {
            menuRoot = gameObject;
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        OpenMenu();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }
    }

    public void OpenMenu()
    {
        SetGameplayEnabled(false);
        Time.timeScale = 0f;

        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = false;
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }
    }

    public void StartGame()
    {
        SetGameplayEnabled(true);
        Time.timeScale = 1f;

        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = true;
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        foreach (MonoBehaviour behaviour in gameplayBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = enabled;
            }
        }
    }
}
