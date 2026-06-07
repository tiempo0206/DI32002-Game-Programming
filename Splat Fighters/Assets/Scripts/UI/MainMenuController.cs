using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the prefab-authored main menu flow and saves selections before entering the gameplay scene.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(170)]
public sealed class MainMenuController : MonoBehaviour
{
    private enum GraphicsPreset
    {
        Performant,
        Balanced,
        HighFidelity
    }

    private enum MenuButtonSound
    {
        None,
        Click,
        Confirm,
        Back,
        Selection
    }

    private const string GraphicsPresetPrefKey = "SplatFighters.Menu.GraphicsPreset";
    private const string FullscreenPrefKey = "SplatFighters.Menu.Fullscreen";
    private const string MatchModePrefKey = "SplatFighters.Menu.MatchMode";
    private const string GameplaySceneName = "MVP_ShootingTest";
    private const string MainMenuCanvasPrefabResource = "UI/MainMenu/Prefabs/MainMenuCanvas";
    private const string CharacterCardResourceRoot = "UI/CharacterSelection/Cards/";

    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private PerformanceProfile performanceProfile = null;
    [SerializeField] private ScoreUI scoreUI = null;
    [SerializeField] private bool hideHudWhileMenuOpen = true;
    [SerializeField] private bool applySavedSettingsOnAwake = true;
    [SerializeField] private MainMenuView menuViewPrefab = null;
    [SerializeField] private MainMenuView menuView = null;

    private GameObject backdropObject;
    private GameObject characterSelectionPreviewStage;
    private CanvasGroup menuGroup;
    private CanvasGroup setupGroup;
    private CanvasGroup settingsGroup;
    private CanvasGroup instructionsGroup;
    private CanvasGroup characterSelectionGroup;
    private Text titleCyanText;
    private Text titlePinkText;
    private Text titleText;
    private Text statusText;
    private Text modeText;
    private Text hintText;
    private Text setupSummaryText;
    private Text settingsSummaryText;
    private Text masterVolumeValueText;
    private Text musicVolumeValueText;
    private Text sfxVolumeValueText;
    private Text playerCharacterText;
    private Text opponentCharacterText;
    private Image playerCharacterCardImage;
    private Image opponentCharacterCardImage;
    private Button primaryButton;
    private Button secondaryButton;
    private Button modeButton;
    private Button difficultyButton;
    private Button setupModeButton;
    private Button setupDifficultyButton;
    private Button continueToFightersButton;
    private Button setupBackButton;
    private Button instructionsButton;
    private Button instructionsBackButton;
    private Button settingsButton;
    private Button settingsBackButton;
    private Button fullscreenButton;
    private Button performantButton;
    private Button balancedButton;
    private Button highFidelityButton;
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Button quitButton;
    private Button previousPlayerCharacterButton;
    private Button nextPlayerCharacterButton;
    private Button previousOpponentCharacterButton;
    private Button nextOpponentCharacterButton;
    private Button confirmCharacterSelectionButton;
    private Button cancelCharacterSelectionButton;
    private bool setupVisible;
    private bool settingsVisible;
    private bool instructionsVisible;
    private bool characterSelectionVisible;
    private bool fullscreenEnabled;
    private GraphicsPreset selectedPreset;
    private GameManager.MatchMode selectedMatchMode;
    private BotDifficulty selectedDifficulty;
    private GameManager.MatchState lastKnownState = GameManager.MatchState.WaitingToStart;
    private GameManager boundGameManager;
    private CharacterVisualCatalog characterCatalog;
    private CharacterPreviewPresenter playerPreview;
    private CharacterPreviewPresenter opponentPreview;
    private Renderer playerPreviewPlatformRenderer;
    private Renderer opponentPreviewPlatformRenderer;
    private SpriteRenderer playerPreviewCardRenderer;
    private SpriteRenderer opponentPreviewCardRenderer;
    private Sprite[] characterCardSprites;
    private int selectedPlayerCharacterIndex;
    private int selectedOpponentCharacterIndex;
    private bool menuViewBound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<MainMenuController>() != null || FindObjectOfType<GameManager>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("MainMenuController");
        controllerObject.AddComponent<MainMenuController>();
    }

    private void Awake()
    {
        selectedPreset = LoadGraphicsPreset();
        fullscreenEnabled = PlayerPrefs.GetInt(FullscreenPrefKey, 0) == 1;
        selectedMatchMode = LoadMatchMode();
        selectedDifficulty = BotDifficultySettings.LoadSavedDifficulty();
        characterCatalog = CharacterVisualCatalog.LoadDefault();
        LoadCharacterSelections();
        ApplyFullscreenMode();
    }

    private void Start()
    {
        EnsureBindings();
        EnsureEventSystem();
        EnsureMenuView();

        if (applySavedSettingsOnAwake)
        {
            ApplySelectedPreset();
        }

        RefreshFromGameState(true);
    }

    private void Update()
    {
        EnsureBindings();

        RefreshFromGameState(false);

        if ((setupVisible || settingsVisible || instructionsVisible || characterSelectionVisible) && Input.GetKeyDown(KeyCode.Escape))
        {
            SplatAudioManager.PlayUiBackSound();
            ShowMainMenu();
        }

        if (gameManager == null && characterSelectionVisible)
        {
            HandleCharacterSelectionInput();
        }
        else if (gameManager == null && setupVisible && Input.GetKeyDown(KeyCode.Return))
        {
            ShowCharacterSelection(true);
        }
        else if (gameManager == null && !settingsVisible && !instructionsVisible && Input.GetKeyDown(KeyCode.Return))
        {
            HandlePrimaryAction();
        }
    }

    private void OnDestroy()
    {
        if (boundGameManager != null)
        {
            boundGameManager.MatchStateChanged -= HandleMatchStateChanged;
            boundGameManager = null;
        }
    }

    private void EnsureBindings()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        if (performanceProfile == null)
        {
            performanceProfile = FindObjectOfType<PerformanceProfile>();
        }

        if (scoreUI == null)
        {
            scoreUI = FindObjectOfType<ScoreUI>();
        }

        if (gameManager != boundGameManager)
        {
            if (boundGameManager != null)
            {
                boundGameManager.MatchStateChanged -= HandleMatchStateChanged;
            }

            if (gameManager != null)
            {
                gameManager.MatchStateChanged += HandleMatchStateChanged;
            }

            boundGameManager = gameManager;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void EnsureMenuView()
    {
        if (menuViewBound)
        {
            return;
        }

        if (menuView == null)
        {
            MainMenuView view = FindObjectOfType<MainMenuView>();

            if (view == null)
            {
                if (menuViewPrefab == null)
                {
                    menuViewPrefab = Resources.Load<MainMenuView>(MainMenuCanvasPrefabResource);
                }

                if (menuViewPrefab != null)
                {
                    view = Instantiate(menuViewPrefab);
                    view.name = menuViewPrefab.name;
                }
            }

            menuView = view;
        }

        if (menuView == null)
        {
            Debug.LogError("Main menu view prefab is missing. Assign MainMenuCanvas.prefab or place a MainMenuView in the scene.", this);
            enabled = false;
            return;
        }

        BindView(menuView);
        RegisterButtonHandlers();
        LoadCharacterCardSprites();
        menuViewBound = true;

        UpdatePresetVisuals();
        ShowMainMenu();
    }

    private void BindView(MainMenuView view)
    {
        backdropObject = view.BackdropObject;
        menuGroup = view.MenuGroup;
        setupGroup = view.SetupGroup;
        settingsGroup = view.SettingsGroup;
        instructionsGroup = view.InstructionsGroup;
        characterSelectionGroup = view.CharacterSelectionGroup;
        titleCyanText = view.TitleCyanText;
        titlePinkText = view.TitlePinkText;
        titleText = view.TitleText;
        statusText = view.StatusText;
        modeText = view.ModeText;
        hintText = view.HintText;
        setupSummaryText = view.SetupSummaryText;
        settingsSummaryText = view.SettingsSummaryText;
        masterVolumeValueText = view.MasterVolumeValueText;
        musicVolumeValueText = view.MusicVolumeValueText;
        sfxVolumeValueText = view.SfxVolumeValueText;
        playerCharacterText = view.PlayerCharacterText;
        opponentCharacterText = view.OpponentCharacterText;
        playerCharacterCardImage = view.PlayerCharacterCardImage;
        opponentCharacterCardImage = view.OpponentCharacterCardImage;
        characterCardSprites = view.CharacterCardSprites;
        primaryButton = view.PrimaryButton;
        secondaryButton = view.SecondaryButton;
        modeButton = view.ModeButton;
        difficultyButton = view.DifficultyButton;
        setupModeButton = view.SetupModeButton;
        setupDifficultyButton = view.SetupDifficultyButton;
        continueToFightersButton = view.ContinueToFightersButton;
        setupBackButton = view.SetupBackButton;
        instructionsButton = view.InstructionsButton;
        instructionsBackButton = view.InstructionsBackButton;
        settingsButton = view.SettingsButton;
        settingsBackButton = view.SettingsBackButton;
        fullscreenButton = view.FullscreenButton;
        performantButton = view.PerformantButton;
        balancedButton = view.BalancedButton;
        highFidelityButton = view.HighFidelityButton;
        masterVolumeSlider = view.MasterVolumeSlider;
        musicVolumeSlider = view.MusicVolumeSlider;
        sfxVolumeSlider = view.SfxVolumeSlider;
        quitButton = view.QuitButton;
        previousPlayerCharacterButton = view.PreviousPlayerCharacterButton;
        nextPlayerCharacterButton = view.NextPlayerCharacterButton;
        previousOpponentCharacterButton = view.PreviousOpponentCharacterButton;
        nextOpponentCharacterButton = view.NextOpponentCharacterButton;
        confirmCharacterSelectionButton = view.ConfirmCharacterSelectionButton;
        cancelCharacterSelectionButton = view.CancelCharacterSelectionButton;
    }

    private void RegisterButtonHandlers()
    {
        ConfigureButton(primaryButton, HandlePrimaryAction, MenuButtonSound.Confirm);
        ConfigureButton(secondaryButton, HandleSecondaryAction, MenuButtonSound.Selection);
        ConfigureButton(modeButton, HandleModeAction, MenuButtonSound.Selection);
        ConfigureButton(difficultyButton, HandleDifficultyAction, MenuButtonSound.Selection);
        ConfigureButton(setupModeButton, HandleSetupModeAction, MenuButtonSound.Selection);
        ConfigureButton(setupDifficultyButton, HandleSetupDifficultyAction, MenuButtonSound.Selection);
        ConfigureButton(continueToFightersButton, () => ShowCharacterSelection(true), MenuButtonSound.Confirm);
        ConfigureButton(setupBackButton, () => ShowSetup(false), MenuButtonSound.Back);
        ConfigureButton(instructionsButton, () => ShowInstructions(true));
        ConfigureButton(instructionsBackButton, () => ShowInstructions(false), MenuButtonSound.Back);
        ConfigureButton(settingsButton, () => ShowSettings(true));
        ConfigureButton(settingsBackButton, () => ShowSettings(false), MenuButtonSound.Back);
        ConfigureButton(fullscreenButton, ToggleFullscreen, MenuButtonSound.Selection);
        ConfigureButton(performantButton, () => SelectPreset(GraphicsPreset.Performant), MenuButtonSound.Selection);
        ConfigureButton(balancedButton, () => SelectPreset(GraphicsPreset.Balanced), MenuButtonSound.Selection);
        ConfigureButton(highFidelityButton, () => SelectPreset(GraphicsPreset.HighFidelity), MenuButtonSound.Selection);
        ConfigureButton(quitButton, HandleQuitAction, MenuButtonSound.Back);
        ConfigureButton(previousPlayerCharacterButton, () => SelectPlayerCharacter(selectedPlayerCharacterIndex - 1), MenuButtonSound.None);
        ConfigureButton(nextPlayerCharacterButton, () => SelectPlayerCharacter(selectedPlayerCharacterIndex + 1), MenuButtonSound.None);
        ConfigureButton(previousOpponentCharacterButton, () => SelectOpponentCharacter(selectedOpponentCharacterIndex - 1), MenuButtonSound.None);
        ConfigureButton(nextOpponentCharacterButton, () => SelectOpponentCharacter(selectedOpponentCharacterIndex + 1), MenuButtonSound.None);
        ConfigureButton(confirmCharacterSelectionButton, ConfirmCharacterSelection, MenuButtonSound.Confirm);
        ConfigureButton(cancelCharacterSelectionButton, () => ShowSetup(true), MenuButtonSound.Back);
        RegisterAudioControlHandlers();
    }

    private static void ConfigureButton(Button button, UnityEngine.Events.UnityAction action, MenuButtonSound sound = MenuButtonSound.Click)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            PlayMenuButtonSound(sound);
            action?.Invoke();
        });
        SetButtonTextColor(button);
    }

    private void RegisterAudioControlHandlers()
    {
        ConfigureVolumeSlider(masterVolumeSlider, SplatAudioManager.GetMasterVolume(), HandleMasterVolumeChanged);
        ConfigureVolumeSlider(musicVolumeSlider, SplatAudioManager.GetMusicVolume(), HandleMusicVolumeChanged);
        ConfigureVolumeSlider(sfxVolumeSlider, SplatAudioManager.GetSfxVolume(), HandleSfxVolumeChanged);
        UpdateAudioControls();
    }

    private static void ConfigureVolumeSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
    }

    private void RefreshFromGameState(bool force)
    {
        if (gameManager == null)
        {
            UpdateMenuText();
            SetVisible(menuGroup, !setupVisible && !settingsVisible && !instructionsVisible && !characterSelectionVisible);
            SetVisible(setupGroup, setupVisible);
            SetVisible(settingsGroup, settingsVisible);
            SetVisible(instructionsGroup, instructionsVisible);
            SetVisible(characterSelectionGroup, characterSelectionVisible);
            SetBackdropVisible(!characterSelectionVisible);
            RefreshCharacterSelectionPresentation();
            SetButtonVisible(modeButton, false);
            SetButtonVisible(secondaryButton, false);
            SetButtonVisible(difficultyButton, false);
            SetButtonVisible(instructionsButton, true);
            if (hideHudWhileMenuOpen && scoreUI != null)
            {
                scoreUI.gameObject.SetActive(false);
            }
            return;
        }

        GameManager.MatchState currentState = gameManager.CurrentState;

        if (!force && currentState == lastKnownState)
        {
            UpdateMenuText();
            UpdateSettingsSummary();
            return;
        }

        lastKnownState = currentState;
        UpdateMenuText();

        bool showMenu = currentState != GameManager.MatchState.Playing;
        if (!showMenu)
        {
            setupVisible = false;
            settingsVisible = false;
        }

        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        SetButtonVisible(modeButton, false);
        SetButtonVisible(secondaryButton, showMenu);
        SetButtonVisible(difficultyButton, currentState == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(instructionsButton, currentState == GameManager.MatchState.WaitingToStart);

        if (hideHudWhileMenuOpen && scoreUI != null)
        {
            scoreUI.gameObject.SetActive(!showMenu);
        }
    }

    private void HandleMatchStateChanged(GameManager.MatchState state)
    {
        lastKnownState = state;
        UpdateMenuText();

        bool showMenu = state != GameManager.MatchState.Playing;
        if (!showMenu)
        {
            setupVisible = false;
            settingsVisible = false;
        }

        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        SetButtonVisible(modeButton, false);
        SetButtonVisible(secondaryButton, showMenu);
        SetButtonVisible(difficultyButton, state == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(instructionsButton, state == GameManager.MatchState.WaitingToStart);

        if (hideHudWhileMenuOpen && scoreUI != null)
        {
            scoreUI.gameObject.SetActive(!showMenu);
        }
    }

    private void HandlePrimaryAction()
    {
        if (gameManager == null)
        {
            ShowSetup(true);
            return;
        }

        switch (gameManager.CurrentState)
        {
            case GameManager.MatchState.WaitingToStart:
                gameManager.StartMatch();
                break;
            case GameManager.MatchState.Paused:
                gameManager.ResumeMatch();
                break;
            case GameManager.MatchState.Finished:
                gameManager.RestartMatch();
                break;
        }
    }

    private void HandleSecondaryAction()
    {
        if (gameManager == null)
        {
            CycleMenuMatchMode();
            return;
        }

        switch (gameManager.CurrentState)
        {
            case GameManager.MatchState.WaitingToStart:
                gameManager.CycleMatchMode();
                break;
            case GameManager.MatchState.Paused:
            case GameManager.MatchState.Finished:
                gameManager.ResetMatch();
                break;
        }
    }

    private void HandleModeAction()
    {
        if (gameManager == null)
        {
            CycleMenuMatchMode();
            return;
        }

        gameManager.CycleMatchMode();
        UpdateMenuText();
    }

    private void HandleDifficultyAction()
    {
        selectedDifficulty = BotDifficultySettings.GetNextDifficulty(selectedDifficulty);
        BotDifficultySettings.SaveDifficulty(selectedDifficulty);
        ApplyDifficultyToActiveBot();
        UpdateMenuText();
        UpdateButtonLabels();
    }

    private void HandleSetupModeAction()
    {
        CycleMenuMatchMode();
    }

    private void HandleSetupDifficultyAction()
    {
        HandleDifficultyAction();
    }

    private void HandleQuitAction()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowSettings(bool visible)
    {
        setupVisible = false;
        settingsVisible = visible;
        instructionsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateButtonLabels();
        UpdateAudioControls();
    }

    private void ShowInstructions(bool visible)
    {
        setupVisible = false;
        instructionsVisible = visible;
        settingsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu && !instructionsVisible);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, showMenu && instructionsVisible);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateButtonLabels();
    }

    private void ShowSetup(bool visible)
    {
        setupVisible = visible;
        instructionsVisible = false;
        settingsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu && !setupVisible);
        SetVisible(setupGroup, showMenu && setupVisible);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateMenuText();
        UpdateButtonLabels();
    }

    private void ShowCharacterSelection(bool visible)
    {
        setupVisible = false;
        characterSelectionVisible = visible;
        instructionsVisible = false;
        settingsVisible = false;
        SetVisible(menuGroup, !characterSelectionVisible);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, characterSelectionVisible);
        SetBackdropVisible(!characterSelectionVisible);
        RefreshCharacterSelectionPresentation();
        UpdateButtonLabels();
    }

    private void ShowMainMenu()
    {
        setupVisible = false;
        instructionsVisible = false;
        settingsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu);
        SetVisible(setupGroup, false);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateButtonLabels();
    }

    private void SelectPreset(GraphicsPreset preset)
    {
        selectedPreset = preset;
        PlayerPrefs.SetInt(GraphicsPresetPrefKey, (int)preset);
        PlayerPrefs.Save();
        ApplySelectedPreset();
        UpdatePresetVisuals();
        UpdateSettingsSummary();
    }

    private void ToggleFullscreen()
    {
        fullscreenEnabled = !fullscreenEnabled;
        PlayerPrefs.SetInt(FullscreenPrefKey, fullscreenEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFullscreenMode();
        UpdatePresetVisuals();
        UpdateSettingsSummary();
    }

    private void ApplySelectedPreset()
    {
        if (performanceProfile == null)
        {
            return;
        }

        switch (selectedPreset)
        {
            case GraphicsPreset.Performant:
                performanceProfile.ApplySettings(30, true, 0.03f);
                break;
            case GraphicsPreset.Balanced:
                performanceProfile.ApplySettings(45, true, 0.02f);
                break;
            case GraphicsPreset.HighFidelity:
                performanceProfile.ApplySettings(60, false, 0.0166667f);
                break;
        }
    }

    private void ApplyFullscreenMode()
    {
        Screen.fullScreen = fullscreenEnabled;
    }

    private void UpdateMenuText()
    {
        string titleLabel = "Splat Fighters";

        if (gameManager != null)
        {
            switch (gameManager.CurrentState)
            {
                case GameManager.MatchState.Playing:
                    titleLabel = "Match Live";
                    break;
                case GameManager.MatchState.Paused:
                    titleLabel = "Paused";
                    break;
                case GameManager.MatchState.Finished:
                    titleLabel = "Match Complete";
                    break;
            }
        }

        SetText(titleText, titleLabel);
        SetText(titleCyanText, titleLabel);
        SetText(titlePinkText, titleLabel);

        if (statusText != null)
        {
            if (gameManager == null)
            {
                statusText.text = "Choose a mode, then start the game.";
            }
            else
            {
                switch (gameManager.CurrentState)
                {
                    case GameManager.MatchState.WaitingToStart:
                        statusText.text = "Ready to start.";
                        break;
                    case GameManager.MatchState.Playing:
                        statusText.text = "The match is live.";
                        break;
                    case GameManager.MatchState.Paused:
                        statusText.text = "Paused. Resume or restart from here.";
                        break;
                    case GameManager.MatchState.Finished:
                        statusText.text = BuildFinishText();
                        break;
                }
            }
        }

        if (modeText != null)
        {
            modeText.text = gameManager == null ? string.Empty : $"Mode: {GetMatchModeLabel(GetCurrentMatchMode())} | AI: {BotDifficultySettings.GetLabel(GetCurrentDifficulty())}";
        }

        if (setupSummaryText != null)
        {
            setupSummaryText.text = $"Mode: {GetMatchModeLabel(GetCurrentMatchMode())}\nAI: {BotDifficultySettings.GetLabel(GetCurrentDifficulty())}";
        }

        if (hintText != null)
        {
            if (gameManager == null)
            {
                hintText.text = settingsVisible
                    ? "Settings are saved for the next game launch."
                    : "Press Start Game to choose fighters and enter the arena.";
            }
            else
            {
                hintText.text = gameManager.CurrentState == GameManager.MatchState.WaitingToStart
                    ? "Enter starts the match. Tune mode and AI difficulty before play."
                    : "Esc pauses during play. Use the settings screen for performance tuning.";
            }
        }

        UpdateButtonLabels();
    }

    private string BuildFinishText()
    {
        switch (gameManager.WinningTeam)
        {
            case Team.TeamA:
                return "Team A wins the round.";
            case Team.TeamB:
                return "Team B wins the round.";
            default:
                return "The round ended in a tie.";
        }
    }

    private void UpdateButtonLabels()
    {
        if (primaryButton != null)
        {
            SetButtonText(primaryButton, GetPrimaryLabel());
        }

        if (secondaryButton != null)
        {
            SetButtonText(secondaryButton, GetSecondaryLabel());
        }

        if (modeButton != null)
        {
            SetButtonText(modeButton, $"Mode: {GetMatchModeLabel(GetCurrentMatchMode())}");
        }

        if (difficultyButton != null)
        {
            SetButtonText(difficultyButton, $"AI Difficulty: {BotDifficultySettings.GetLabel(GetCurrentDifficulty())}");
        }

        if (setupModeButton != null)
        {
            SetButtonText(setupModeButton, $"Mode: {GetMatchModeLabel(GetCurrentMatchMode())}");
        }

        if (setupDifficultyButton != null)
        {
            SetButtonText(setupDifficultyButton, $"AI Difficulty: {BotDifficultySettings.GetLabel(GetCurrentDifficulty())}");
        }

        if (settingsButton != null)
        {
            SetButtonText(settingsButton, settingsVisible ? "Close Settings" : "Settings");
        }
    }

    private string GetPrimaryLabel()
    {
        if (gameManager == null)
        {
            return "Start Game";
        }

        switch (gameManager.CurrentState)
        {
            case GameManager.MatchState.WaitingToStart:
                return "Start Match";
            case GameManager.MatchState.Paused:
                return "Resume";
            case GameManager.MatchState.Finished:
                return "Restart";
            default:
                return "Start Match";
        }
    }

    private string GetSecondaryLabel()
    {
        if (gameManager == null)
        {
            return "Mode Selection";
        }

        switch (gameManager.CurrentState)
        {
            case GameManager.MatchState.WaitingToStart:
                return "Mode Selection";
            case GameManager.MatchState.Paused:
            case GameManager.MatchState.Finished:
                return "Reset Match";
            default:
                return "Mode Selection";
        }
    }

    private static string GetMatchModeLabel(GameManager.MatchMode mode)
    {
        switch (mode)
        {
            case GameManager.MatchMode.TurfWar:
                return "Turf War";
            case GameManager.MatchMode.SplatZones:
                return "Splat Zones";
            case GameManager.MatchMode.TowerControl:
                return "Tower Control";
            default:
                return mode.ToString();
        }
    }

    private void UpdatePresetVisuals()
    {
        SetButtonHighlight(performantButton, selectedPreset == GraphicsPreset.Performant);
        SetButtonHighlight(balancedButton, selectedPreset == GraphicsPreset.Balanced);
        SetButtonHighlight(highFidelityButton, selectedPreset == GraphicsPreset.HighFidelity);

        if (fullscreenButton != null)
        {
            SetButtonText(fullscreenButton, fullscreenEnabled ? "Fullscreen: On" : "Fullscreen: Off");
        }
    }

    private void UpdateSettingsSummary()
    {
        if (settingsSummaryText == null)
        {
            return;
        }

        settingsSummaryText.text =
            $"Preset: {selectedPreset} | Fullscreen: {(fullscreenEnabled ? "On" : "Off")}\n"
            + $"Audio: Master {FormatVolume(SplatAudioManager.GetMasterVolume())} | Music {FormatVolume(SplatAudioManager.GetMusicVolume())} | SFX {FormatVolume(SplatAudioManager.GetSfxVolume())}";
    }

    private void HandleMasterVolumeChanged(float value)
    {
        SplatAudioManager.SetMasterVolumeValue(value);
        UpdateAudioVolumeLabels();
        UpdateSettingsSummary();
    }

    private void HandleMusicVolumeChanged(float value)
    {
        SplatAudioManager.SetMusicVolumeValue(value);
        UpdateAudioVolumeLabels();
        UpdateSettingsSummary();
    }

    private void HandleSfxVolumeChanged(float value)
    {
        SplatAudioManager.SetSfxVolumeValue(value);
        UpdateAudioVolumeLabels();
        UpdateSettingsSummary();
    }

    private void UpdateAudioControls()
    {
        SetSliderValue(masterVolumeSlider, SplatAudioManager.GetMasterVolume());
        SetSliderValue(musicVolumeSlider, SplatAudioManager.GetMusicVolume());
        SetSliderValue(sfxVolumeSlider, SplatAudioManager.GetSfxVolume());
        UpdateAudioVolumeLabels();
        UpdateSettingsSummary();
    }

    private void UpdateAudioVolumeLabels()
    {
        SetText(masterVolumeValueText, FormatVolume(SplatAudioManager.GetMasterVolume()));
        SetText(musicVolumeValueText, FormatVolume(SplatAudioManager.GetMusicVolume()));
        SetText(sfxVolumeValueText, FormatVolume(SplatAudioManager.GetSfxVolume()));
    }

    private void CycleMenuMatchMode()
    {
        selectedMatchMode = selectedMatchMode == GameManager.MatchMode.TowerControl
            ? GameManager.MatchMode.TurfWar
            : (GameManager.MatchMode)((int)selectedMatchMode + 1);
        PlayerPrefs.SetInt(MatchModePrefKey, (int)selectedMatchMode);
        PlayerPrefs.Save();
        UpdateMenuText();
        UpdateButtonLabels();
    }

    private BotDifficulty GetCurrentDifficulty()
    {
        return selectedDifficulty;
    }

    private void ApplyDifficultyToActiveBot()
    {
        BotController bot = FindObjectOfType<BotController>();

        if (bot != null)
        {
            bot.SetDifficulty(selectedDifficulty);
        }
    }

    private void LoadCharacterSelections()
    {
        int legacyIndex = PlayerPrefs.GetInt("SplatFighters.SelectedCharacter", 5);
        selectedPlayerCharacterIndex = NormalizeCharacterIndex(PlayerPrefs.GetInt(CharacterSelectionManager.PlayerCharacterPrefsKey, legacyIndex));
        selectedOpponentCharacterIndex = EnsureDistinctOpponentIndex(PlayerPrefs.GetInt(CharacterSelectionManager.OpponentCharacterPrefsKey, selectedPlayerCharacterIndex + 1));
    }

    private void HandleCharacterSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectPlayerCharacter(selectedPlayerCharacterIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SelectPlayerCharacter(selectedPlayerCharacterIndex + 1);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectOpponentCharacter(selectedOpponentCharacterIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectOpponentCharacter(selectedOpponentCharacterIndex + 1);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SplatAudioManager.PlayUiConfirmSound();
            ConfirmCharacterSelection();
        }
    }

    private void SelectPlayerCharacter(int index)
    {
        int previousIndex = selectedPlayerCharacterIndex;
        selectedPlayerCharacterIndex = NormalizeCharacterIndex(index);

        if (selectedPlayerCharacterIndex == selectedOpponentCharacterIndex && characterCatalog != null && characterCatalog.Count > 1)
        {
            selectedOpponentCharacterIndex = EnsureDistinctOpponentIndex(selectedOpponentCharacterIndex + 1);

            if (opponentPreview != null)
            {
                opponentPreview.Select(selectedOpponentCharacterIndex);
            }
        }

        if (playerPreview != null)
        {
            playerPreview.Select(selectedPlayerCharacterIndex);
        }

        if (selectedPlayerCharacterIndex != previousIndex)
        {
            SplatAudioManager.PlaySelectionMoveSound();
        }

        RefreshCharacterSelectionPresentation();
    }

    private void SelectOpponentCharacter(int index)
    {
        int previousIndex = selectedOpponentCharacterIndex;
        int direction = index < selectedOpponentCharacterIndex ? -1 : 1;
        selectedOpponentCharacterIndex = EnsureDistinctOpponentIndex(index, direction);

        if (opponentPreview != null)
        {
            opponentPreview.Select(selectedOpponentCharacterIndex);
        }

        if (selectedOpponentCharacterIndex != previousIndex)
        {
            SplatAudioManager.PlaySelectionMoveSound();
        }

        RefreshCharacterSelectionPresentation();
    }

    private void ConfirmCharacterSelection()
    {
        PlayerPrefs.SetInt(CharacterSelectionManager.PlayerCharacterPrefsKey, selectedPlayerCharacterIndex);
        PlayerPrefs.SetInt(CharacterSelectionManager.OpponentCharacterPrefsKey, selectedOpponentCharacterIndex);
        BotDifficultySettings.SaveDifficulty(selectedDifficulty);
        TeamVisualPalette.SaveSelectedColor(Team.TeamA, GetCharacterInkColor(selectedPlayerCharacterIndex));
        TeamVisualPalette.SaveSelectedColor(Team.TeamB, GetCharacterInkColor(selectedOpponentCharacterIndex));
        PlayerPrefs.Save();
        SceneManager.LoadScene(GameplaySceneName, LoadSceneMode.Single);
    }

    private int NormalizeCharacterIndex(int index)
    {
        return characterCatalog != null && characterCatalog.Count > 0 ? characterCatalog.NormalizeIndex(index) : 0;
    }

    private int EnsureDistinctOpponentIndex(int index, int direction = 1)
    {
        int normalized = NormalizeCharacterIndex(index);
        return characterCatalog != null && characterCatalog.Count > 1 && normalized == selectedPlayerCharacterIndex
            ? NormalizeCharacterIndex(normalized + (direction < 0 ? -1 : 1))
            : normalized;
    }

    private void EnsureCharacterSelectionPreviewStage()
    {
        if (characterSelectionPreviewStage != null || characterCatalog == null || characterCatalog.Count == 0)
        {
            return;
        }

        characterSelectionPreviewStage = new GameObject("CharacterSelectionPreviewStage");

        GameObject lightObject = new GameObject("PreviewDirectionalLight");
        lightObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        lightObject.transform.rotation = Quaternion.Euler(35f, -25f, 0f);
        Light previewLight = lightObject.AddComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.25f;

        playerPreviewCardRenderer = CreatePreviewCard("PlayerCardBackdrop", new Vector3(-2.6f, -0.45f, 0.72f), selectedPlayerCharacterIndex);
        opponentPreviewCardRenderer = CreatePreviewCard("OpponentCardBackdrop", new Vector3(2.6f, -0.45f, 0.72f), selectedOpponentCharacterIndex);
        playerPreview = CreateCharacterPreview("PlayerPreview", new Vector3(-5.28f, -2.6f, -0.22f), Team.TeamA, selectedPlayerCharacterIndex);
        opponentPreview = CreateCharacterPreview("OpponentPreview", new Vector3(5.28f, -2.6f, -0.22f), Team.TeamB, selectedOpponentCharacterIndex);
        playerPreviewPlatformRenderer = CreatePreviewPlatform("PlayerPlatform", new Vector3(-5.28f, -2.76f, -0.22f), selectedPlayerCharacterIndex);
        opponentPreviewPlatformRenderer = CreatePreviewPlatform("OpponentPlatform", new Vector3(5.28f, -2.76f, -0.22f), selectedOpponentCharacterIndex);
    }

    private SpriteRenderer CreatePreviewCard(string name, Vector3 position, int characterIndex)
    {
        GameObject cardObject = new GameObject(name);
        cardObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        cardObject.transform.position = position;

        SpriteRenderer cardRenderer = cardObject.AddComponent<SpriteRenderer>();
        cardRenderer.sprite = GetCharacterCardSprite(characterIndex);
        cardRenderer.color = new Color(1f, 1f, 1f, 0.92f);
        cardRenderer.sortingOrder = -20;
        FitPreviewCard(cardRenderer);
        return cardRenderer;
    }

    private CharacterPreviewPresenter CreateCharacterPreview(string name, Vector3 position, Team team, int index)
    {
        GameObject previewObject = new GameObject(name);
        previewObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        previewObject.transform.position = position;

        CharacterPreviewPresenter preview = previewObject.AddComponent<CharacterPreviewPresenter>();
        preview.Configure(characterCatalog, team, index);
        preview.ConfigureStageFit(2.55f, 1.55f);
        return preview;
    }

    private Renderer CreatePreviewPlatform(string name, Vector3 position, int characterIndex)
    {
        GameObject platformObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformObject.name = name;
        platformObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        platformObject.transform.position = position;
        platformObject.transform.localScale = new Vector3(1.32f, 0.11f, 1.32f);

        Collider platformCollider = platformObject.GetComponent<Collider>();
        if (platformCollider != null)
        {
            Destroy(platformCollider);
        }

        Renderer platformRenderer = platformObject.GetComponent<Renderer>();
        Shader platformShader = Shader.Find("Universal Render Pipeline/Lit");
        if (platformRenderer != null && platformShader != null)
        {
            Material platformMaterial = new Material(platformShader);
            Color color = GetCharacterInkColor(characterIndex);
            platformMaterial.color = color;
            platformMaterial.SetColor("_BaseColor", color);
            platformRenderer.sharedMaterial = platformMaterial;
        }

        return platformRenderer;
    }

    private void SetCharacterSelectionStageVisible(bool visible)
    {
        if (visible)
        {
            EnsureCharacterSelectionPreviewStage();
        }

        if (characterSelectionPreviewStage != null)
        {
            characterSelectionPreviewStage.SetActive(visible);
        }
    }

    private void RefreshCharacterSelectionPresentation()
    {
        SetCharacterSelectionStageVisible(characterSelectionVisible);
        UpdateCharacterSelectionText();
    }

    private void LoadCharacterCardSprites()
    {
        if (characterCardSprites != null && characterCardSprites.Length > 0)
        {
            return;
        }

        int cardCount = characterCatalog != null ? characterCatalog.Count : 0;
        characterCardSprites = new Sprite[cardCount];

        for (int i = 0; i < cardCount; i++)
        {
            string displayName = GetCharacterDisplayName(i);
            string resourceName = BuildCharacterCardResourceName(displayName);

            if (!string.IsNullOrEmpty(resourceName))
            {
                characterCardSprites[i] = Resources.Load<Sprite>($"{CharacterCardResourceRoot}{resourceName}");
            }
        }
    }

    private void UpdateCharacterSelectionText()
    {
        LoadCharacterCardSprites();
        UpdateCharacterCardImage(playerCharacterCardImage, selectedPlayerCharacterIndex);
        UpdateCharacterCardImage(opponentCharacterCardImage, selectedOpponentCharacterIndex);
        UpdatePreviewCardSprite(playerPreviewCardRenderer, selectedPlayerCharacterIndex);
        UpdatePreviewCardSprite(opponentPreviewCardRenderer, selectedOpponentCharacterIndex);

        if (playerCharacterText != null)
        {
            string name = playerPreview != null ? playerPreview.CurrentDisplayName : GetCharacterDisplayName(selectedPlayerCharacterIndex);
            Color inkColor = GetCharacterInkColor(selectedPlayerCharacterIndex);
            bool hasCharacterCard = HasCharacterCard(selectedPlayerCharacterIndex);
            playerCharacterText.gameObject.SetActive(!hasCharacterCard);
            playerCharacterText.text = hasCharacterCard ? string.Empty : $"TEAM A PLAYER\n{name}";
            playerCharacterText.color = inkColor;
            UpdatePreviewPlatformColor(playerPreviewPlatformRenderer, inkColor);
        }

        if (opponentCharacterText != null)
        {
            string name = opponentPreview != null ? opponentPreview.CurrentDisplayName : GetCharacterDisplayName(selectedOpponentCharacterIndex);
            Color inkColor = GetCharacterInkColor(selectedOpponentCharacterIndex);
            bool hasCharacterCard = HasCharacterCard(selectedOpponentCharacterIndex);
            opponentCharacterText.gameObject.SetActive(!hasCharacterCard);
            opponentCharacterText.text = hasCharacterCard ? string.Empty : $"TEAM B OPPONENT\n{name}";
            opponentCharacterText.color = inkColor;
            UpdatePreviewPlatformColor(opponentPreviewPlatformRenderer, inkColor);
        }
    }

    private void UpdateCharacterCardImage(Image image, int characterIndex)
    {
        if (image == null)
        {
            return;
        }

        Sprite cardSprite = GetCharacterCardSprite(characterIndex);
        bool usingWorldPreviewCards = characterSelectionPreviewStage != null && characterSelectionPreviewStage.activeSelf;
        if (usingWorldPreviewCards)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = cardSprite;
        image.enabled = cardSprite != null;
        image.color = Color.white;
    }

    private void UpdatePreviewCardSprite(SpriteRenderer renderer, int characterIndex)
    {
        if (renderer == null)
        {
            return;
        }

        Sprite cardSprite = GetCharacterCardSprite(characterIndex);
        renderer.sprite = cardSprite;
        renderer.enabled = cardSprite != null;
        renderer.color = new Color(1f, 1f, 1f, 0.92f);
        FitPreviewCard(renderer);
    }

    private static void FitPreviewCard(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null || renderer.sprite.bounds.size.y <= Mathf.Epsilon)
        {
            return;
        }

        const float targetHeight = 4.6f;
        float scale = targetHeight / renderer.sprite.bounds.size.y;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private bool HasCharacterCard(int index)
    {
        return GetCharacterCardSprite(index) != null;
    }

    private Sprite GetCharacterCardSprite(int index)
    {
        LoadCharacterCardSprites();

        if (characterCardSprites == null || characterCardSprites.Length == 0)
        {
            return null;
        }

        int normalizedIndex = NormalizeCharacterIndex(index);
        return normalizedIndex >= 0 && normalizedIndex < characterCardSprites.Length ? characterCardSprites[normalizedIndex] : null;
    }

    private static string BuildCharacterCardResourceName(string displayName)
    {
        return string.IsNullOrEmpty(displayName) ? string.Empty : $"{displayName.Replace(" ", string.Empty)}Card";
    }

    private string GetCharacterDisplayName(int index)
    {
        CharacterVisualOption option = characterCatalog != null ? characterCatalog.GetOption(index) : null;
        return option != null ? option.DisplayName : "Unavailable";
    }

    private Color GetCharacterInkColor(int index)
    {
        CharacterVisualOption option = characterCatalog != null ? characterCatalog.GetOption(index) : null;
        return option != null ? option.InkColor : Color.white;
    }

    private static void UpdatePreviewPlatformColor(Renderer platformRenderer, Color inkColor)
    {
        if (platformRenderer == null || platformRenderer.sharedMaterial == null)
        {
            return;
        }

        platformRenderer.sharedMaterial.color = inkColor;

        if (platformRenderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            platformRenderer.sharedMaterial.SetColor("_BaseColor", inkColor);
        }
    }

    private GameManager.MatchMode GetCurrentMatchMode()
    {
        return gameManager != null ? gameManager.CurrentMatchMode : selectedMatchMode;
    }

    private GraphicsPreset LoadGraphicsPreset()
    {
        int rawPreset = PlayerPrefs.GetInt(GraphicsPresetPrefKey, (int)GraphicsPreset.Performant);

        if (rawPreset < (int)GraphicsPreset.Performant || rawPreset > (int)GraphicsPreset.HighFidelity)
        {
            return GraphicsPreset.Performant;
        }

        return (GraphicsPreset)rawPreset;
    }

    private GameManager.MatchMode LoadMatchMode()
    {
        int rawMode = PlayerPrefs.GetInt(MatchModePrefKey, (int)GameManager.MatchMode.TurfWar);

        if (rawMode < (int)GameManager.MatchMode.TurfWar || rawMode > (int)GameManager.MatchMode.TowerControl)
        {
            return GameManager.MatchMode.TurfWar;
        }

        return (GameManager.MatchMode)rawMode;
    }

    private static void SetButtonText(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);

        if (text != null)
        {
            text.text = label;
            text.color = Color.white;
        }
    }

    private static void SetButtonTextColor(Button button)
    {
        if (button == null)
        {
            return;
        }

        foreach (Text text in button.GetComponentsInChildren<Text>(true))
        {
            text.color = Color.white;
        }
    }

    private static void PlayMenuButtonSound(MenuButtonSound sound)
    {
        switch (sound)
        {
            case MenuButtonSound.Click:
                SplatAudioManager.PlayUiClickSound();
                break;
            case MenuButtonSound.Confirm:
                SplatAudioManager.PlayUiConfirmSound();
                break;
            case MenuButtonSound.Back:
                SplatAudioManager.PlayUiBackSound();
                break;
            case MenuButtonSound.Selection:
                SplatAudioManager.PlaySelectionMoveSound();
                break;
        }
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }

    private static string FormatVolume(float value)
    {
        return $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    private static void SetButtonHighlight(Button button, bool highlighted)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();

        if (image == null)
        {
            return;
        }

        image.color = highlighted ? new Color(0.26f, 0.42f, 0.65f, 0.98f) : new Color(0.2f, 0.24f, 0.28f, 0.95f);
    }

    private static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    private void SetBackdropVisible(bool visible)
    {
        if (backdropObject != null)
        {
            backdropObject.SetActive(visible);
        }
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
        {
            button.gameObject.SetActive(visible);
        }
    }
}
