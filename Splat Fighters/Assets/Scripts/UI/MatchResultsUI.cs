using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime results overlay shown when the match reaches the finished state.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(185)]
public sealed class MatchResultsUI : MonoBehaviour
{
    private const float CoverageBarWidth = 420f;

    [Header("References")]
    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private CanvasGroup resultsGroup = null;
    [SerializeField] private Text titleText = null;
    [SerializeField] private Text winnerText = null;
    [SerializeField] private Text modeText = null;
    [SerializeField] private Text scoreText = null;
    [SerializeField] private Text objectiveText = null;
    [SerializeField] private Text controlsText = null;
    [SerializeField] private Text teamALabelText = null;
    [SerializeField] private Text teamBLabelText = null;
    [SerializeField] private RectTransform teamABarFill = null;
    [SerializeField] private RectTransform teamBBarFill = null;
    [SerializeField] private Image teamABarImage = null;
    [SerializeField] private Image teamBBarImage = null;
    [SerializeField] private Button restartButton = null;
    [SerializeField] private Button resetButton = null;
    [SerializeField] private Button closeButton = null;
    [SerializeField] private bool manageCursor = true;

    private GameManager boundGameManager;
    private SplatZoneObjective zoneObjective;
    private TowerObjective towerObjective;
    private bool visible;

    public static MatchResultsUI CreateRuntimeResultsUI()
    {
        GameObject canvasObject = new GameObject("MatchResultsCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        CanvasGroup group = canvasObject.AddComponent<CanvasGroup>();

        GameObject backdropObject = CreateImageObject(canvasObject.transform, "ResultsBackdrop", new Color(0.01f, 0.015f, 0.02f, 0.72f));
        RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
        StretchToParent(backdropRect);

        GameObject panelObject = CreateImageObject(canvasObject.transform, "ResultsPanel", new Color(0.03f, 0.045f, 0.06f, 0.96f));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(820f, 620f);

        MatchResultsUI resultsUI = canvasObject.AddComponent<MatchResultsUI>();
        resultsUI.resultsGroup = group;
        resultsUI.titleText = CreateText(panelObject.transform, "ResultsTitleText", new Vector2(0f, -42f), new Vector2(740f, 62f), 42, FontStyle.Bold, TextAnchor.MiddleCenter);
        resultsUI.winnerText = CreateText(panelObject.transform, "ResultsWinnerText", new Vector2(0f, -110f), new Vector2(740f, 44f), 25, FontStyle.Bold, TextAnchor.MiddleCenter);
        resultsUI.modeText = CreateText(panelObject.transform, "ResultsModeText", new Vector2(0f, -158f), new Vector2(740f, 36f), 22, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.scoreText = CreateText(panelObject.transform, "ResultsScoreText", new Vector2(0f, -204f), new Vector2(740f, 44f), 26, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateCoverageBar(panelObject.transform, "TeamACoverageBar", new Vector2(260f, -282f), Team.TeamA, out resultsUI.teamALabelText, out resultsUI.teamABarFill, out resultsUI.teamABarImage);
        CreateCoverageBar(panelObject.transform, "TeamBCoverageBar", new Vector2(260f, -348f), Team.TeamB, out resultsUI.teamBLabelText, out resultsUI.teamBBarFill, out resultsUI.teamBBarImage);

        resultsUI.objectiveText = CreateText(panelObject.transform, "ResultsObjectiveText", new Vector2(0f, -414f), new Vector2(720f, 76f), 20, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.controlsText = CreateText(panelObject.transform, "ResultsControlsText", new Vector2(0f, -504f), new Vector2(720f, 32f), 17, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.restartButton = CreateButton(panelObject.transform, "RestartButton", "Restart Match", new Vector2(-245f, -560f), new Vector2(210f, 56f), TeamVisualPalette.GetColor(Team.TeamA));
        resultsUI.resetButton = CreateButton(panelObject.transform, "ResetButton", "Reset Match", new Vector2(0f, -560f), new Vector2(210f, 56f), new Color(0.18f, 0.22f, 0.27f, 1f));
        resultsUI.closeButton = CreateButton(panelObject.transform, "CloseButton", "Close Results", new Vector2(245f, -560f), new Vector2(210f, 56f), TeamVisualPalette.GetColor(Team.TeamB));
        resultsUI.ConfigureButtons();
        resultsUI.SetVisible(false);

        return resultsUI;
    }

    public void Bind(GameManager manager)
    {
        if (boundGameManager == manager)
        {
            return;
        }

        if (boundGameManager != null)
        {
            boundGameManager.MatchStateChanged -= HandleMatchStateChanged;
        }

        boundGameManager = manager;
        gameManager = manager;

        if (boundGameManager != null)
        {
            boundGameManager.MatchStateChanged += HandleMatchStateChanged;
        }

        RefreshVisibility();
    }

    private void Start()
    {
        EnsureEventSystem();
        ConfigureButtons();

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        Bind(gameManager);
    }

    private void OnDestroy()
    {
        if (boundGameManager != null)
        {
            boundGameManager.MatchStateChanged -= HandleMatchStateChanged;
            boundGameManager = null;
        }
    }

    private void Update()
    {
        if (gameManager == null)
        {
            Bind(GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>());
        }

        if (!visible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R))
        {
            HandleRestartClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            HandleResetClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetVisible(false);
        }
    }

    private void HandleMatchStateChanged(GameManager.MatchState state)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool shouldShow = gameManager != null && gameManager.CurrentState == GameManager.MatchState.Finished;

        if (shouldShow)
        {
            RefreshResults();
        }

        SetVisible(shouldShow);
    }

    private void RefreshResults()
    {
        if (gameManager == null)
        {
            return;
        }

        Team winningTeam = gameManager.WinningTeam;
        float teamACoverage = Mathf.Clamp(gameManager.TeamACoverage, 0f, 100f);
        float teamBCoverage = Mathf.Clamp(gameManager.TeamBCoverage, 0f, 100f);
        float margin = Mathf.Abs(teamACoverage - teamBCoverage);

        if (titleText != null)
        {
            titleText.text = winningTeam == Team.None ? "Draw" : $"{TeamVisualPalette.GetLabel(winningTeam)} Wins";
            titleText.color = winningTeam == Team.None ? Color.white : TeamVisualPalette.GetColor(winningTeam);
        }

        if (winnerText != null)
        {
            winnerText.text = winningTeam == Team.None ? "Both teams finished with equal painted coverage." : $"{TeamVisualPalette.GetLabel(winningTeam)} wins by {margin:0.0} percentage points.";
            winnerText.color = Color.white;
        }

        if (modeText != null)
        {
            modeText.text = $"Mode: {FormatMatchMode(gameManager.CurrentMatchMode)}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Final Paint: {TeamVisualPalette.TeamALabel} {teamACoverage:0.0}%  |  {TeamVisualPalette.TeamBLabel} {teamBCoverage:0.0}%";
        }

        UpdateCoverageBar(teamALabelText, teamABarFill, teamABarImage, Team.TeamA, teamACoverage);
        UpdateCoverageBar(teamBLabelText, teamBBarFill, teamBBarImage, Team.TeamB, teamBCoverage);

        if (objectiveText != null)
        {
            objectiveText.text = FormatObjectiveSummary();
        }

        if (controlsText != null)
        {
            controlsText.text = "Enter/R: restart   Backspace: reset   Esc: close results";
        }
    }

    private string FormatObjectiveSummary()
    {
        if (gameManager == null)
        {
            return string.Empty;
        }

        switch (gameManager.CurrentMatchMode)
        {
            case GameManager.MatchMode.SplatZones:
                ResolveObjectiveReferences();
                if (zoneObjective == null)
                {
                    return "Splat Zones summary unavailable.";
                }

                return $"Zone control: {FormatControlState(zoneObjective.ControllingTeam, zoneObjective.IsContested)} | Zone paint {TeamVisualPalette.TeamALabel} {zoneObjective.TeamAPercent:0}% / {TeamVisualPalette.TeamBLabel} {zoneObjective.TeamBPercent:0}%";
            case GameManager.MatchMode.TowerControl:
                ResolveObjectiveReferences();
                if (towerObjective == null)
                {
                    return "Tower Control summary unavailable.";
                }

                string leadLabel = towerObjective.LeadingTeam == Team.None ? "No tower lead" : $"{TeamVisualPalette.GetLabel(towerObjective.LeadingTeam)} led the tower";
                return $"{leadLabel} {towerObjective.RouteProgressPercent:0}% | Tower control: {FormatControlState(towerObjective.ControllingTeam, towerObjective.IsContested)}";
            default:
                float margin = Mathf.Abs(gameManager.TeamACoverage - gameManager.TeamBCoverage);
                return $"Turf War is decided by final painted ground coverage. Paint margin: {margin:0.0} percentage points.";
        }
    }

    private void ResolveObjectiveReferences()
    {
        if (zoneObjective == null)
        {
            zoneObjective = FindObjectOfType<SplatZoneObjective>();
        }

        if (towerObjective == null)
        {
            towerObjective = FindObjectOfType<TowerObjective>();
        }
    }

    private static string FormatControlState(Team team, bool contested)
    {
        if (contested)
        {
            return "Contested";
        }

        return team == Team.None ? "Neutral" : TeamVisualPalette.GetLabel(team);
    }

    private static string FormatMatchMode(GameManager.MatchMode mode)
    {
        switch (mode)
        {
            case GameManager.MatchMode.SplatZones:
                return "Splat Zones";
            case GameManager.MatchMode.TowerControl:
                return "Tower Control";
            default:
                return "Turf War";
        }
    }

    private void UpdateCoverageBar(Text label, RectTransform fill, Image fillImage, Team team, float percent)
    {
        Color teamColor = TeamVisualPalette.GetColor(team);

        if (label != null)
        {
            label.text = $"{TeamVisualPalette.GetLabel(team)} {percent:0.0}%";
            label.color = teamColor;
        }

        if (fillImage != null)
        {
            fillImage.color = teamColor;
        }

        if (fill != null)
        {
            fill.sizeDelta = new Vector2(Mathf.Clamp01(percent / 100f) * CoverageBarWidth, fill.sizeDelta.y);
        }
    }

    private void SetVisible(bool shouldShow)
    {
        visible = shouldShow;

        if (resultsGroup != null)
        {
            resultsGroup.alpha = shouldShow ? 1f : 0f;
            resultsGroup.interactable = shouldShow;
            resultsGroup.blocksRaycasts = shouldShow;
        }

        if (manageCursor && shouldShow)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ConfigureButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(HandleRestartClicked);
            restartButton.onClick.AddListener(HandleRestartClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(HandleResetClicked);
            resetButton.onClick.AddListener(HandleResetClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }
    }

    private void HandleRestartClicked()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.RestartMatch();

        if (manageCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void HandleResetClicked()
    {
        if (gameManager != null)
        {
            gameManager.ResetMatch();
        }
    }

    private void HandleCloseClicked()
    {
        SetVisible(false);
    }

    private static GameObject CreateImageObject(Transform parent, string name, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        imageObject.AddComponent<RectTransform>();
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return imageObject;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static void CreateCoverageBar(Transform parent, string name, Vector2 anchoredPosition, Team team, out Text label, out RectTransform fillRect, out Image fillImage)
    {
        GameObject rowObject = new GameObject(name);
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(720f, 48f);

        label = CreateText(rowObject.transform, $"{name}Label", new Vector2(-244f, -3f), new Vector2(250f, 36f), 21, FontStyle.Bold, TextAnchor.MiddleRight);

        GameObject trackObject = CreateImageObject(rowObject.transform, $"{name}Track", new Color(1f, 1f, 1f, 0.12f));
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 1f);
        trackRect.anchorMax = new Vector2(0f, 1f);
        trackRect.pivot = new Vector2(0f, 1f);
        trackRect.anchoredPosition = new Vector2(300f, -8f);
        trackRect.sizeDelta = new Vector2(CoverageBarWidth, 26f);

        GameObject fillObject = CreateImageObject(trackObject.transform, $"{name}Fill", TeamVisualPalette.GetColor(team));
        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = new Vector2(-CoverageBarWidth, 0f);
        fillRect.sizeDelta = new Vector2(0f, 0f);
        fillImage = fillObject.GetComponent<Image>();
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject buttonObject = CreateImageObject(parent, name, color);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.Lerp(color, Color.black, 0.45f);
        button.colors = colors;

        Text buttonText = CreateText(buttonObject.transform, $"{name}Text", Vector2.zero, size, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;
        buttonText.text = label;
        buttonText.color = Color.white;

        return button;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
