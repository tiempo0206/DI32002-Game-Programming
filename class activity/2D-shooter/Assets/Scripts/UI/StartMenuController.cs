using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the start menu overlay, instructions page, and music handoff into gameplay.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    public static bool startImmediatelyOnNextLoad = false;

    [Tooltip("The menu root to hide once gameplay starts.")]
    [SerializeField] private GameObject menuRoot = null;

    [Tooltip("The main menu page that contains the start and instructions actions.")]
    [SerializeField] private GameObject mainMenuPage = null;

    [Tooltip("The instructions page shown from the main menu.")]
    [SerializeField] private GameObject instructionsPage = null;

    [Tooltip("The button that starts the game.")]
    [SerializeField] private Button startButton = null;

    [Tooltip("The button that opens the instructions page.")]
    [SerializeField] private Button instructionsButton = null;

    [Tooltip("The button that returns from instructions to the main menu page.")]
    [SerializeField] private Button backButton = null;

    [Tooltip("Additional menu flow buttons that should play the menu click sound when pressed.")]
    [SerializeField] private Button[] clickFeedbackButtons = new Button[0];

    [Tooltip("The menu music audio source.")]
    [SerializeField] private AudioSource menuMusicSource = null;

    [Tooltip("The gameplay music audio source.")]
    [SerializeField] private AudioSource gameplayMusicSource = null;

    [Tooltip("Optional one-shot sound for menu button presses.")]
    [SerializeField] private AudioClip menuClickSound = null;

    [Tooltip("Optional in-game objective label to refresh at runtime.")]
    [SerializeField] private TMP_Text objectiveText = null;

    [Tooltip("Objective text shown during the playable portion of the level.")]
    [SerializeField] [TextArea] private string runtimeObjectiveDescription = "MISSION: Destroy 15 raiders. Blue pickups trigger temporary speed and fire boosts.";

    [Tooltip("Volume of the menu click sound.")]
    [SerializeField] private float menuClickVolume = 0.85f;

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

        if (instructionsButton != null)
        {
            instructionsButton.onClick.AddListener(OpenInstructions);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowMainMenuPage);
        }

        RegisterClickFeedbackButtons();

        OpenMenu();
        UpdateObjectiveText();

        if (startImmediatelyOnNextLoad)
        {
            startImmediatelyOnNextLoad = false;
            BeginGame(false);
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (instructionsButton != null)
        {
            instructionsButton.onClick.RemoveListener(OpenInstructions);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowMainMenuPage);
        }

        UnregisterClickFeedbackButtons();
    }

    public void OpenMenu()
    {
        SetGameplayEnabled(false);
        Time.timeScale = 0f;
        SetMenuVisible(true);
        ShowMainMenuPage(false);
        SetMusicForGameplay(false);

        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = false;
        }
    }

    public void OpenInstructions()
    {
        PlayMenuClick();

        if (mainMenuPage != null)
        {
            mainMenuPage.SetActive(false);
        }

        if (instructionsPage != null)
        {
            instructionsPage.SetActive(true);
        }
    }

    public void ShowMainMenuPage()
    {
        ShowMainMenuPage(true);
    }

    public void StartGame()
    {
        BeginGame(true);
    }

    private void BeginGame(bool playMenuClick)
    {
        if (playMenuClick)
        {
            PlayMenuClick();
        }

        SetGameplayEnabled(true);
        Time.timeScale = 1f;
        SetMenuVisible(false);
        SetMusicForGameplay(true);

        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = false;
        }
    }

    private void ShowMainMenuPage(bool playMenuClick)
    {
        if (playMenuClick)
        {
            PlayMenuClick();
        }

        if (mainMenuPage != null)
        {
            mainMenuPage.SetActive(true);
        }

        if (instructionsPage != null)
        {
            instructionsPage.SetActive(false);
        }
    }

    private void SetMenuVisible(bool isVisible)
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(isVisible);
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

    private void SetMusicForGameplay(bool gameplayActive)
    {
        if (menuMusicSource != null)
        {
            if (gameplayActive)
            {
                menuMusicSource.Stop();
            }
            else if (!menuMusicSource.isPlaying)
            {
                menuMusicSource.Play();
            }
        }

        if (gameplayMusicSource != null)
        {
            if (gameplayActive)
            {
                if (!gameplayMusicSource.isPlaying)
                {
                    gameplayMusicSource.Play();
                }
            }
            else
            {
                gameplayMusicSource.Stop();
            }
        }
    }

    private void UpdateObjectiveText()
    {
        if (objectiveText == null)
        {
            GameObject objectiveObject = GameObject.Find("ObjectiveText");
            if (objectiveObject != null)
            {
                objectiveText = objectiveObject.GetComponent<TMP_Text>();
            }
        }

        if (objectiveText != null)
        {
            objectiveText.text = runtimeObjectiveDescription;
        }
    }

    private void PlayMenuClick()
    {
        if (menuClickSound == null)
        {
            return;
        }

        Vector3 soundPosition = Vector3.zero;
        if (Camera.main != null)
        {
            soundPosition = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(menuClickSound, soundPosition, menuClickVolume);
    }

    private void RegisterClickFeedbackButtons()
    {
        foreach (Button button in clickFeedbackButtons)
        {
            if (button != null)
            {
                button.onClick.AddListener(PlayMenuClick);
            }
        }
    }

    private void UnregisterClickFeedbackButtons()
    {
        foreach (Button button in clickFeedbackButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayMenuClick);
            }
        }
    }
}
