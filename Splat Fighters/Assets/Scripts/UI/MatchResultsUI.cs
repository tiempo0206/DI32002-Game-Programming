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
    private const float TowerRouteBarWidth = 500f;

    [Header("References")]
    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private CanvasGroup resultsGroup = null;
    [SerializeField] private Image outcomeStripeImage = null;
    [SerializeField] private Text titleText = null;
    [SerializeField] private Text subtitleText = null;
    [SerializeField] private Text winnerText = null;
    [SerializeField] private Text modeText = null;
    [SerializeField] private Text scoreText = null;
    [SerializeField] private Text objectiveText = null;
    [SerializeField] private Text controlsText = null;
    [SerializeField] private Text teamALabelText = null;
    [SerializeField] private Text teamBLabelText = null;
    [SerializeField] private Text towerRouteLabelText = null;
    [SerializeField] private RectTransform teamABarFill = null;
    [SerializeField] private RectTransform teamBBarFill = null;
    [SerializeField] private RectTransform towerRouteFill = null;
    [SerializeField] private Image teamABarImage = null;
    [SerializeField] private Image teamBBarImage = null;
    [SerializeField] private Image towerRouteImage = null;
    [SerializeField] private Button restartButton = null;
    [SerializeField] private Button resetButton = null;
    [SerializeField] private Button closeButton = null;
    [SerializeField] private bool manageCursor = true;

    private GameManager boundGameManager;
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

        GameObject panelObject = CreateImageObject(canvasObject.transform, "ResultsPanel", new Color(0.025f, 0.032f, 0.045f, 0.97f));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(900f, 680f);

        MatchResultsUI resultsUI = canvasObject.AddComponent<MatchResultsUI>();
        resultsUI.resultsGroup = group;
        GameObject stripeObject = CreateImageObject(panelObject.transform, "ResultsOutcomeStripe", new Color(1f, 1f, 1f, 0.88f));
        RectTransform stripeRect = stripeObject.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0.5f, 1f);
        stripeRect.anchorMax = new Vector2(0.5f, 1f);
        stripeRect.pivot = new Vector2(0.5f, 1f);
        stripeRect.anchoredPosition = new Vector2(0f, -14f);
        stripeRect.sizeDelta = new Vector2(812f, 10f);
        resultsUI.outcomeStripeImage = stripeObject.GetComponent<Image>();

        resultsUI.titleText = CreateText(panelObject.transform, "ResultsTitleText", new Vector2(0f, -38f), new Vector2(800f, 64f), 46, FontStyle.Bold, TextAnchor.MiddleCenter);
        resultsUI.subtitleText = CreateText(panelObject.transform, "ResultsSubtitleText", new Vector2(0f, -100f), new Vector2(800f, 34f), 20, FontStyle.Bold, TextAnchor.MiddleCenter);
        resultsUI.winnerText = CreateText(panelObject.transform, "ResultsWinnerText", new Vector2(0f, -138f), new Vector2(800f, 42f), 25, FontStyle.Bold, TextAnchor.MiddleCenter);
        resultsUI.modeText = CreateText(panelObject.transform, "ResultsModeText", new Vector2(0f, -184f), new Vector2(800f, 34f), 21, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.scoreText = CreateText(panelObject.transform, "ResultsScoreText", new Vector2(0f, -226f), new Vector2(800f, 42f), 25, FontStyle.Bold, TextAnchor.MiddleCenter);

        CreateCoverageBar(panelObject.transform, "TeamACoverageBar", new Vector2(260f, -300f), Team.TeamA, out resultsUI.teamALabelText, out resultsUI.teamABarFill, out resultsUI.teamABarImage);
        CreateCoverageBar(panelObject.transform, "TeamBCoverageBar", new Vector2(260f, -364f), Team.TeamB, out resultsUI.teamBLabelText, out resultsUI.teamBBarFill, out resultsUI.teamBBarImage);
        CreateTowerRouteBar(panelObject.transform, "TowerRouteBar", new Vector2(0f, -430f), out resultsUI.towerRouteLabelText, out resultsUI.towerRouteFill, out resultsUI.towerRouteImage);

        resultsUI.objectiveText = CreateText(panelObject.transform, "ResultsObjectiveText", new Vector2(0f, -488f), new Vector2(780f, 78f), 20, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.controlsText = CreateText(panelObject.transform, "ResultsControlsText", new Vector2(0f, -570f), new Vector2(780f, 28f), 17, FontStyle.Normal, TextAnchor.MiddleCenter);
        resultsUI.restartButton = CreateButton(panelObject.transform, "RestartButton", "Restart Match", new Vector2(-255f, -622f), new Vector2(220f, 56f), TeamVisualPalette.GetColor(Team.TeamA));
        resultsUI.resetButton = CreateButton(panelObject.transform, "ResetButton", "Reset Match", new Vector2(0f, -622f), new Vector2(220f, 56f), new Color(0.18f, 0.22f, 0.27f, 1f));
        resultsUI.closeButton = CreateButton(panelObject.transform, "CloseButton", "Close Results", new Vector2(255f, -622f), new Vector2(220f, 56f), TeamVisualPalette.GetColor(Team.TeamB));
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
        Color outcomeColor = winningTeam == Team.None ? Color.white : TeamVisualPalette.GetColor(winningTeam);

        if (outcomeStripeImage != null)
        {
            outcomeStripeImage.color = new Color(outcomeColor.r, outcomeColor.g, outcomeColor.b, 0.92f);
        }

        if (titleText != null)
        {
            titleText.text = winningTeam == Team.None ? "Draw" : $"{TeamVisualPalette.GetLabel(winningTeam)} Wins";
            titleText.color = outcomeColor;
        }

        if (subtitleText != null)
        {
            subtitleText.text = FormatResultSubtitle(winningTeam);
            subtitleText.color = Color.Lerp(outcomeColor, Color.white, winningTeam == Team.None ? 0f : 0.35f);
        }

        if (winnerText != null)
        {
            winnerText.text = FormatWinnerLine(winningTeam, margin);
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
        UpdateTowerRouteBar();

        if (objectiveText != null)
        {
            objectiveText.text = FormatObjectiveSummary();
        }

        if (controlsText != null)
        {
            controlsText.text = "Enter/R: restart   Backspace: reset   Esc: close results";
        }
    }

    private string FormatResultSubtitle(Team winningTeam)
    {
        if (gameManager == null)
        {
            return string.Empty;
        }

        switch (gameManager.CurrentMatchMode)
        {
            case GameManager.MatchMode.TowerControl:
                if (winningTeam == Team.None)
                {
                    return "Tower stayed balanced";
                }

                ResolveObjectiveReferences();
                return towerObjective != null && towerObjective.GoalTeam == winningTeam
                    ? "Tower reached the goal"
                    : "Best tower push wins";
            default:
                return winningTeam == Team.None ? "Equal paint coverage" : "Final paint coverage lead";
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
            case GameManager.MatchMode.TowerControl:
                ResolveObjectiveReferences();
                if (towerObjective == null)
                {
                    return "Tower Control summary unavailable.";
                }

                string progressLabel = towerObjective.GoalTeam == Team.None
                    ? $"{FormatTowerLead()} | Tower control: {FormatControlState(towerObjective.ControllingTeam, towerObjective.IsContested)}"
                    : $"{TeamVisualPalette.GetLabel(towerObjective.GoalTeam)} reached the goal.";
                return $"{progressLabel} | Tower paint {TeamVisualPalette.TeamALabel} {towerObjective.TeamAPercent:0}% / {TeamVisualPalette.TeamBLabel} {towerObjective.TeamBPercent:0}%";
            default:
                float margin = Mathf.Abs(gameManager.TeamACoverage - gameManager.TeamBCoverage);
                return $"Turf War is decided by final painted ground coverage. Paint margin: {margin:0.0} percentage points.";
        }
    }

    private void ResolveObjectiveReferences()
    {
        if (towerObjective == null)
        {
            towerObjective = FindObjectOfType<TowerObjective>();
        }
    }

    private string FormatWinnerLine(Team winningTeam, float paintMargin)
    {
        if (gameManager == null)
        {
            return string.Empty;
        }

        if (gameManager.CurrentMatchMode != GameManager.MatchMode.TowerControl)
        {
            return winningTeam == Team.None ? "Both teams finished with equal painted coverage." : $"{TeamVisualPalette.GetLabel(winningTeam)} wins by {paintMargin:0.0} percentage points.";
        }

        ResolveObjectiveReferences();

        if (winningTeam == Team.None)
        {
            return "The tower stayed even with no control advantage.";
        }

        if (towerObjective != null && towerObjective.GoalTeam == winningTeam)
        {
            return $"{TeamVisualPalette.GetLabel(winningTeam)} wins by pushing the tower to the goal.";
        }

        return towerObjective != null
            ? $"{TeamVisualPalette.GetLabel(winningTeam)} wins with the better tower push."
            : $"{TeamVisualPalette.GetLabel(winningTeam)} wins the Tower Control round.";
    }

    private string FormatTowerLead()
    {
        if (towerObjective == null || towerObjective.LeadingTeam == Team.None)
        {
            return "No tower lead";
        }

        return $"{TeamVisualPalette.GetLabel(towerObjective.LeadingTeam)} led the tower {towerObjective.RouteProgressPercent:0}%";
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

    private void UpdateTowerRouteBar()
    {
        bool showTowerRoute = gameManager != null && gameManager.CurrentMatchMode == GameManager.MatchMode.TowerControl;
        GameObject routeRow = towerRouteLabelText != null && towerRouteLabelText.transform.parent != null
            ? towerRouteLabelText.transform.parent.gameObject
            : null;

        if (routeRow != null)
        {
            routeRow.SetActive(showTowerRoute);
        }

        if (!showTowerRoute)
        {
            return;
        }

        ResolveObjectiveReferences();

        if (towerObjective == null)
        {
            if (towerRouteLabelText != null)
            {
                towerRouteLabelText.text = "Tower route unavailable";
                towerRouteLabelText.color = Color.white;
            }

            if (towerRouteFill != null)
            {
                towerRouteFill.sizeDelta = new Vector2(0f, towerRouteFill.sizeDelta.y);
            }

            return;
        }

        Team routeTeam = towerObjective.GoalTeam != Team.None ? towerObjective.GoalTeam : towerObjective.LeadingTeam;
        Color routeColor = routeTeam == Team.None ? Color.white : TeamVisualPalette.GetColor(routeTeam);
        float progressPercent = towerObjective.RouteProgressPercent;

        if (towerRouteLabelText != null)
        {
            string leadLabel = routeTeam == Team.None ? "Center" : TeamVisualPalette.GetLabel(routeTeam);
            towerRouteLabelText.text = $"Tower Route: {leadLabel} {progressPercent:0}%";
            towerRouteLabelText.color = routeColor;
        }

        if (towerRouteImage != null)
        {
            towerRouteImage.color = routeColor;
        }

        if (towerRouteFill != null)
        {
            towerRouteFill.sizeDelta = new Vector2(Mathf.Clamp01(progressPercent / 100f) * TowerRouteBarWidth, towerRouteFill.sizeDelta.y);
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
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);
        fillImage = fillObject.GetComponent<Image>();
    }

    private static void CreateTowerRouteBar(Transform parent, string name, Vector2 anchoredPosition, out Text label, out RectTransform fillRect, out Image fillImage)
    {
        GameObject rowObject = new GameObject(name);
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = anchoredPosition;
        rowRect.sizeDelta = new Vector2(760f, 44f);

        label = CreateText(rowObject.transform, $"{name}Label", new Vector2(-250f, -1f), new Vector2(270f, 34f), 20, FontStyle.Bold, TextAnchor.MiddleRight);

        GameObject trackObject = CreateImageObject(rowObject.transform, $"{name}Track", new Color(1f, 1f, 1f, 0.14f));
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 1f);
        trackRect.anchorMax = new Vector2(0f, 1f);
        trackRect.pivot = new Vector2(0f, 1f);
        trackRect.anchoredPosition = new Vector2(310f, -7f);
        trackRect.sizeDelta = new Vector2(TowerRouteBarWidth, 24f);

        GameObject fillObject = CreateImageObject(trackObject.transform, $"{name}Fill", Color.white);
        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
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
