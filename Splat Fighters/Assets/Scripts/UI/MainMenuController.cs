using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the main menu and settings screen at runtime so the project can start from a proper flow without a hand-authored scene UI.
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

    private const string GraphicsPresetPrefKey = "SplatFighters.Menu.GraphicsPreset";
    private const string FullscreenPrefKey = "SplatFighters.Menu.Fullscreen";
    private const string MatchModePrefKey = "SplatFighters.Menu.MatchMode";
    private const string GameplaySceneName = "MVP_ShootingTest";

    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private PerformanceProfile performanceProfile = null;
    [SerializeField] private ScoreUI scoreUI = null;
    [SerializeField] private bool hideHudWhileMenuOpen = true;
    [SerializeField] private bool applySavedSettingsOnAwake = true;

    private Canvas canvas;
    private GameObject backdropObject;
    private GameObject menuPanelObject;
    private GameObject settingsPanelObject;
    private GameObject instructionsPanelObject;
    private GameObject characterSelectionPanelObject;
    private GameObject characterSelectionPreviewStage;
    private CanvasGroup menuGroup;
    private CanvasGroup settingsGroup;
    private CanvasGroup instructionsGroup;
    private CanvasGroup characterSelectionGroup;
    private Text titleText;
    private Text statusText;
    private Text modeText;
    private Text hintText;
    private Text settingsSummaryText;
    private Text playerCharacterText;
    private Text opponentCharacterText;
    private Button primaryButton;
    private Button secondaryButton;
    private Button modeButton;
    private Button difficultyButton;
    private Button instructionsButton;
    private Button settingsButton;
    private Button fullscreenButton;
    private Button performantButton;
    private Button balancedButton;
    private Button highFidelityButton;
    private Button quitButton;
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
    private int selectedPlayerCharacterIndex;
    private int selectedOpponentCharacterIndex;

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
        EnsureRuntimeUi();

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

        if ((settingsVisible || instructionsVisible || characterSelectionVisible) && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowMainMenu();
        }

        if (gameManager == null && characterSelectionVisible)
        {
            HandleCharacterSelectionInput();
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

    private void EnsureRuntimeUi()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MainMenuCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        backdropObject = CreateBackdrop(canvasObject.transform, "Backdrop", new Color(0.035f, 0.055f, 0.085f, 1f));
        backdropObject.transform.SetAsFirstSibling();
        backdropObject.SetActive(false);

        menuPanelObject = CreatePanel(canvasObject.transform, "MenuPanel", new Vector2(0.5f, 0.5f), new Vector2(620f, 720f));
        menuGroup = menuPanelObject.AddComponent<CanvasGroup>();

        settingsPanelObject = CreatePanel(canvasObject.transform, "SettingsPanel", new Vector2(0.5f, 0.5f), new Vector2(620f, 610f));
        settingsGroup = settingsPanelObject.AddComponent<CanvasGroup>();

        instructionsPanelObject = CreatePanel(canvasObject.transform, "InstructionsPanel", new Vector2(0.5f, 0.5f), new Vector2(760f, 690f));
        instructionsGroup = instructionsPanelObject.AddComponent<CanvasGroup>();

        characterSelectionPanelObject = CreateContainer(canvasObject.transform, "CharacterSelectionPanel", new Vector2(0.5f, 0.5f), new Vector2(1280f, 720f));
        characterSelectionGroup = characterSelectionPanelObject.AddComponent<CanvasGroup>();

        titleText = CreateText(menuPanelObject.transform, "TitleText", "Splat Fighters", new Vector2(0f, -42f), 42, FontStyle.Bold, new Vector2(560f, 66f), TextAnchor.UpperCenter);
        CreateText(menuPanelObject.transform, "SubtitleText", "Paint the arena. Control the map. Win the round.", new Vector2(0f, -108f), 19, FontStyle.Italic, new Vector2(560f, 32f), TextAnchor.UpperCenter);
        statusText = CreateText(menuPanelObject.transform, "StatusText", "Ready to start.", new Vector2(0f, -160f), 22, FontStyle.Bold, new Vector2(560f, 34f), TextAnchor.UpperCenter);
        modeText = CreateText(menuPanelObject.transform, "ModeText", "Mode: Turf War", new Vector2(0f, -202f), 19, FontStyle.Normal, new Vector2(560f, 30f), TextAnchor.UpperCenter);
        hintText = CreateText(menuPanelObject.transform, "HintText", "Press Enter or Start Game to enter the arena.", new Vector2(0f, -244f), 17, FontStyle.Normal, new Vector2(560f, 48f), TextAnchor.UpperCenter);

        primaryButton = CreateButton(menuPanelObject.transform, "PrimaryButton", "Start Game", new Vector2(0f, -310f), new Vector2(360f, 52f), HandlePrimaryAction);
        secondaryButton = CreateButton(menuPanelObject.transform, "SecondaryButton", "Cycle Mode", new Vector2(0f, -372f), new Vector2(360f, 46f), HandleSecondaryAction);
        modeButton = CreateButton(menuPanelObject.transform, "ModeButton", "Mode: Turf War", new Vector2(0f, -372f), new Vector2(360f, 46f), HandleModeAction);
        difficultyButton = CreateButton(menuPanelObject.transform, "DifficultyButton", "AI Difficulty: Normal", new Vector2(0f, -426f), new Vector2(360f, 46f), HandleDifficultyAction);
        instructionsButton = CreateButton(menuPanelObject.transform, "InstructionsButton", "How To Play", new Vector2(0f, -480f), new Vector2(360f, 46f), () => ShowInstructions(true));
        settingsButton = CreateButton(menuPanelObject.transform, "SettingsButton", "Settings", new Vector2(0f, -534f), new Vector2(360f, 46f), () => ShowSettings(true));
        quitButton = CreateButton(menuPanelObject.transform, "QuitButton", "Quit", new Vector2(0f, -588f), new Vector2(360f, 46f), HandleQuitAction);
        CreateText(menuPanelObject.transform, "VersionText", "Basic menu version | Visual art pass planned", new Vector2(0f, -656f), 14, FontStyle.Normal, new Vector2(560f, 24f), TextAnchor.UpperCenter);

        CreateText(settingsPanelObject.transform, "SettingsTitleText", "Settings", new Vector2(0f, -30f), 32, FontStyle.Bold, new Vector2(500f, 52f), TextAnchor.UpperCenter);
        settingsSummaryText = CreateText(settingsPanelObject.transform, "SettingsSummaryText", "Preset: Performant | Fullscreen: Off", new Vector2(0f, -88f), 17, FontStyle.Normal, new Vector2(500f, 64f), TextAnchor.UpperCenter);
        CreateText(settingsPanelObject.transform, "GraphicsTitleText", "Graphics Preset", new Vector2(0f, -168f), 22, FontStyle.Bold, new Vector2(500f, 34f), TextAnchor.UpperCenter);

        performantButton = CreateButton(settingsPanelObject.transform, "PerformantButton", "Performant", new Vector2(0f, -220f), new Vector2(330f, 44f), () => SelectPreset(GraphicsPreset.Performant));
        balancedButton = CreateButton(settingsPanelObject.transform, "BalancedButton", "Balanced", new Vector2(0f, -272f), new Vector2(330f, 44f), () => SelectPreset(GraphicsPreset.Balanced));
        highFidelityButton = CreateButton(settingsPanelObject.transform, "HighFidelityButton", "High Fidelity", new Vector2(0f, -324f), new Vector2(330f, 44f), () => SelectPreset(GraphicsPreset.HighFidelity));
        fullscreenButton = CreateButton(settingsPanelObject.transform, "FullscreenButton", "Fullscreen", new Vector2(0f, -398f), new Vector2(330f, 44f), ToggleFullscreen);
        CreateButton(settingsPanelObject.transform, "BackButton", "Back", new Vector2(0f, -450f), new Vector2(330f, 44f), () => ShowSettings(false));

        CreateText(instructionsPanelObject.transform, "InstructionsTitleText", "How To Play", new Vector2(0f, -34f), 36, FontStyle.Bold, new Vector2(700f, 56f), TextAnchor.UpperCenter);
        CreateText(
            instructionsPanelObject.transform,
            "InstructionsText",
            "OBJECTIVE\nPaint more of the arena than Team B before the timer ends.\n\nCONTROLS\nWASD: Move\nMouse: Aim camera\nLeft Mouse: Fire or use active paint tool\nSpace: Jump\nLeft Shift: Swim faster and recover ink while standing on your paint\n1 / 2: Switch between shooter and roller\nQ: Use the special paint burst when ready\nP or Esc: Pause match\nR: Restart match\nM: Cycle demo mode\n\nTIPS\nYour selected fighter defines your ink color. Your paint creates safe movement lanes. Enemy paint slows you down.",
            new Vector2(0f, -112f),
            19,
            FontStyle.Normal,
            new Vector2(700f, 480f),
            TextAnchor.UpperLeft);
        CreateButton(instructionsPanelObject.transform, "InstructionsBackButton", "Back", new Vector2(0f, -606f), new Vector2(360f, 46f), () => ShowInstructions(false));

        CreateText(characterSelectionPanelObject.transform, "CharacterSelectionTitleText", "Select Fighters", new Vector2(0f, -16f), 40, FontStyle.Bold, new Vector2(900f, 56f), TextAnchor.UpperCenter);
        CreateText(characterSelectionPanelObject.transform, "CharacterSelectionHintText", "Choose two different animated fighters. Each fighter has a signature ink color.", new Vector2(0f, -70f), 18, FontStyle.Normal, new Vector2(1000f, 36f), TextAnchor.UpperCenter);
        playerCharacterText = CreateText(characterSelectionPanelObject.transform, "PlayerCharacterText", string.Empty, new Vector2(-320f, -128f), 23, FontStyle.Bold, new Vector2(480f, 64f), TextAnchor.UpperCenter);
        opponentCharacterText = CreateText(characterSelectionPanelObject.transform, "OpponentCharacterText", string.Empty, new Vector2(320f, -128f), 23, FontStyle.Bold, new Vector2(480f, 64f), TextAnchor.UpperCenter);
        CreateButton(characterSelectionPanelObject.transform, "PreviousPlayerCharacterButton", "< Player", new Vector2(-430f, -520f), new Vector2(210f, 46f), () => SelectPlayerCharacter(selectedPlayerCharacterIndex - 1));
        CreateButton(characterSelectionPanelObject.transform, "NextPlayerCharacterButton", "Player >", new Vector2(-190f, -520f), new Vector2(210f, 46f), () => SelectPlayerCharacter(selectedPlayerCharacterIndex + 1));
        CreateButton(characterSelectionPanelObject.transform, "PreviousOpponentCharacterButton", "< Opponent", new Vector2(190f, -520f), new Vector2(210f, 46f), () => SelectOpponentCharacter(selectedOpponentCharacterIndex - 1));
        CreateButton(characterSelectionPanelObject.transform, "NextOpponentCharacterButton", "Opponent >", new Vector2(430f, -520f), new Vector2(210f, 46f), () => SelectOpponentCharacter(selectedOpponentCharacterIndex + 1));
        CreateText(characterSelectionPanelObject.transform, "CharacterSelectionControlText", "A / D: player   Left / Right: opponent   Enter: confirm   Esc: back", new Vector2(0f, -584f), 17, FontStyle.Normal, new Vector2(900f, 30f), TextAnchor.UpperCenter);
        CreateButton(characterSelectionPanelObject.transform, "ConfirmCharacterSelectionButton", "Enter Arena", new Vector2(0f, -630f), new Vector2(300f, 48f), ConfirmCharacterSelection);
        CreateButton(characterSelectionPanelObject.transform, "CancelCharacterSelectionButton", "Back", new Vector2(-470f, -630f), new Vector2(190f, 44f), () => ShowCharacterSelection(false));

        UpdatePresetVisuals();
        ShowMainMenu();
    }

    private void RefreshFromGameState(bool force)
    {
        if (gameManager == null)
        {
            UpdateMenuText();
            SetVisible(menuGroup, !settingsVisible && !instructionsVisible && !characterSelectionVisible);
            SetVisible(settingsGroup, settingsVisible);
            SetVisible(instructionsGroup, instructionsVisible);
            SetVisible(characterSelectionGroup, characterSelectionVisible);
            SetBackdropVisible(!characterSelectionVisible);
            SetCharacterSelectionStageVisible(characterSelectionVisible);
            SetButtonVisible(modeButton, false);
            SetButtonVisible(secondaryButton, true);
            SetButtonVisible(difficultyButton, true);
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
            settingsVisible = false;
        }

        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        SetButtonVisible(modeButton, currentState == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(secondaryButton, currentState == GameManager.MatchState.Paused || currentState == GameManager.MatchState.Finished);
        SetButtonVisible(difficultyButton, currentState == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(instructionsButton, false);

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
            settingsVisible = false;
        }

        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        SetButtonVisible(modeButton, state == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(secondaryButton, state == GameManager.MatchState.Paused || state == GameManager.MatchState.Finished);
        SetButtonVisible(difficultyButton, state == GameManager.MatchState.WaitingToStart);
        SetButtonVisible(instructionsButton, false);

        if (hideHudWhileMenuOpen && scoreUI != null)
        {
            scoreUI.gameObject.SetActive(!showMenu);
        }
    }

    private void HandlePrimaryAction()
    {
        if (gameManager == null)
        {
            ShowCharacterSelection(true);
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

    private void HandleQuitAction()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowSettings(bool visible)
    {
        settingsVisible = visible;
        instructionsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu && !settingsVisible);
        SetVisible(settingsGroup, showMenu && settingsVisible);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateButtonLabels();
    }

    private void ShowInstructions(bool visible)
    {
        instructionsVisible = visible;
        settingsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu && !instructionsVisible);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, showMenu && instructionsVisible);
        SetVisible(characterSelectionGroup, false);
        SetCharacterSelectionStageVisible(false);
        SetBackdropVisible(showMenu);
        UpdateButtonLabels();
    }

    private void ShowCharacterSelection(bool visible)
    {
        characterSelectionVisible = visible;
        instructionsVisible = false;
        settingsVisible = false;
        SetVisible(menuGroup, !characterSelectionVisible);
        SetVisible(settingsGroup, false);
        SetVisible(instructionsGroup, false);
        SetVisible(characterSelectionGroup, characterSelectionVisible);
        SetBackdropVisible(!characterSelectionVisible);
        SetCharacterSelectionStageVisible(characterSelectionVisible);
        UpdateCharacterSelectionText();
        UpdateButtonLabels();
    }

    private void ShowMainMenu()
    {
        instructionsVisible = false;
        settingsVisible = false;
        characterSelectionVisible = false;
        bool showMenu = gameManager == null || gameManager.CurrentState != GameManager.MatchState.Playing;
        SetVisible(menuGroup, showMenu);
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
        if (titleText != null)
        {
            if (gameManager == null)
            {
                titleText.text = "Splat Fighters";
            }
            else
            {
                switch (gameManager.CurrentState)
                {
                    case GameManager.MatchState.WaitingToStart:
                        titleText.text = "Splat Fighters";
                        break;
                    case GameManager.MatchState.Playing:
                        titleText.text = "Match Live";
                        break;
                    case GameManager.MatchState.Paused:
                        titleText.text = "Paused";
                        break;
                    case GameManager.MatchState.Finished:
                        titleText.text = "Match Complete";
                        break;
                }
            }
        }

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
            modeText.text = $"Mode: {GetMatchModeLabel(GetCurrentMatchMode())} | AI: {BotDifficultySettings.GetLabel(GetCurrentDifficulty())}";
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
            return "Cycle Mode";
        }

        switch (gameManager.CurrentState)
        {
            case GameManager.MatchState.WaitingToStart:
                return "Cycle Mode";
            case GameManager.MatchState.Paused:
            case GameManager.MatchState.Finished:
                return "Reset Match";
            default:
                return "Cycle Mode";
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

        settingsSummaryText.text = $"Preset: {selectedPreset} | Fullscreen: {(fullscreenEnabled ? "On" : "Off")}";
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
            ConfirmCharacterSelection();
        }
    }

    private void SelectPlayerCharacter(int index)
    {
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

        UpdateCharacterSelectionText();
    }

    private void SelectOpponentCharacter(int index)
    {
        int direction = index < selectedOpponentCharacterIndex ? -1 : 1;
        selectedOpponentCharacterIndex = EnsureDistinctOpponentIndex(index, direction);

        if (opponentPreview != null)
        {
            opponentPreview.Select(selectedOpponentCharacterIndex);
        }

        UpdateCharacterSelectionText();
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

        playerPreview = CreateCharacterPreview("PlayerPreview", new Vector3(-2.6f, -1.85f, 0f), Team.TeamA, selectedPlayerCharacterIndex);
        opponentPreview = CreateCharacterPreview("OpponentPreview", new Vector3(2.6f, -1.85f, 0f), Team.TeamB, selectedOpponentCharacterIndex);
        playerPreviewPlatformRenderer = CreatePreviewPlatform("PlayerPlatform", new Vector3(-2.6f, -2.05f, 0f), selectedPlayerCharacterIndex);
        opponentPreviewPlatformRenderer = CreatePreviewPlatform("OpponentPlatform", new Vector3(2.6f, -2.05f, 0f), selectedOpponentCharacterIndex);
    }

    private CharacterPreviewPresenter CreateCharacterPreview(string name, Vector3 position, Team team, int index)
    {
        GameObject previewObject = new GameObject(name);
        previewObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        previewObject.transform.position = position;

        CharacterPreviewPresenter preview = previewObject.AddComponent<CharacterPreviewPresenter>();
        preview.Configure(characterCatalog, team, index);
        return preview;
    }

    private Renderer CreatePreviewPlatform(string name, Vector3 position, int characterIndex)
    {
        GameObject platformObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformObject.name = name;
        platformObject.transform.SetParent(characterSelectionPreviewStage.transform, false);
        platformObject.transform.position = position;
        platformObject.transform.localScale = new Vector3(1.55f, 0.14f, 1.55f);

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

    private void UpdateCharacterSelectionText()
    {
        if (playerCharacterText != null)
        {
            string name = playerPreview != null ? playerPreview.CurrentDisplayName : GetCharacterDisplayName(selectedPlayerCharacterIndex);
            Color inkColor = GetCharacterInkColor(selectedPlayerCharacterIndex);
            playerCharacterText.text = $"TEAM A PLAYER\n{name}\nInk #{ColorUtility.ToHtmlStringRGB(inkColor)}";
            playerCharacterText.color = inkColor;
            UpdatePreviewPlatformColor(playerPreviewPlatformRenderer, inkColor);
        }

        if (opponentCharacterText != null)
        {
            string name = opponentPreview != null ? opponentPreview.CurrentDisplayName : GetCharacterDisplayName(selectedOpponentCharacterIndex);
            Color inkColor = GetCharacterInkColor(selectedOpponentCharacterIndex);
            opponentCharacterText.text = $"TEAM B OPPONENT\n{name}\nInk #{ColorUtility.ToHtmlStringRGB(inkColor)}";
            opponentCharacterText.color = inkColor;
            UpdatePreviewPlatformColor(opponentPreviewPlatformRenderer, inkColor);
        }
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

    private static GameObject CreateBackdrop(Transform parent, string name, Color color)
    {
        GameObject backdropObject = new GameObject(name);
        backdropObject.transform.SetParent(parent, false);

        RectTransform rect = backdropObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        Image image = backdropObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return backdropObject;
    }

    private static GameObject CreateContainer(Transform parent, string name, Vector2 anchorPoint, Vector2 sizeDelta)
    {
        GameObject containerObject = new GameObject(name);
        containerObject.transform.SetParent(parent, false);

        RectTransform rect = containerObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorPoint;
        rect.anchorMax = anchorPoint;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        return containerObject;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorPoint, Vector2 sizeDelta)
    {
        GameObject panelObject = CreateContainer(parent, name, anchorPoint, sizeDelta);
        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0.055f, 0.075f, 0.11f, 0.96f);
        return panelObject;
    }

    private static Text CreateText(Transform parent, string name, string text, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle, Vector2 sizeDelta, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text textComponent = textObject.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.color = Color.white;
        textComponent.text = text;
        return textComponent;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.24f, 0.28f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.24f, 0.28f, 0.95f);
        colors.highlightedColor = new Color(0.28f, 0.32f, 0.38f, 0.98f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.14f, 0.16f, 0.2f, 0.6f);
        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 4f);
        labelRect.offsetMax = new Vector2(-10f, -4f);

        Text textComponent = labelObject.AddComponent<Text>();
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 20;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.color = Color.white;
        textComponent.text = label;

        return button;
    }

    private static void SetButtonText(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>();

        if (text != null)
        {
            text.text = label;
        }
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
