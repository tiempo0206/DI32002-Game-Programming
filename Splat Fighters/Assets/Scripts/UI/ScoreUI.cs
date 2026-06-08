using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD for match timer and team paint coverage.
/// It can be wired in the scene or auto-created by GameManager for quick testing.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    private const float HudBarWidth = 356f;
    private const float TowerBarWidth = 476f;

    [Header("Text References")]
    [SerializeField] private Text presentationText = null;
    [SerializeField] private Text timerText = null;
    [SerializeField] private Text teamAText = null;
    [SerializeField] private Text teamBText = null;
    [SerializeField] private Text inkText = null;
    [SerializeField] private Text healthText = null;
    [SerializeField] private Text specialText = null;
    [SerializeField] private Text objectiveText = null;
    [SerializeField] private Text towerText = null;
    [SerializeField] private Text statusText = null;

    [Header("Bar References")]
    [SerializeField] private RectTransform teamACoverageFill = null;
    [SerializeField] private RectTransform teamBCoverageFill = null;
    [SerializeField] private RectTransform towerProgressFill = null;
    [SerializeField] private RectTransform inkFill = null;
    [SerializeField] private RectTransform healthFill = null;
    [SerializeField] private RectTransform specialFill = null;
    [SerializeField] private Image teamACoverageImage = null;
    [SerializeField] private Image teamBCoverageImage = null;
    [SerializeField] private Image towerProgressImage = null;
    [SerializeField] private Image inkImage = null;
    [SerializeField] private Image healthImage = null;
    [SerializeField] private Image specialImage = null;

    [Header("Formatting")]
    [SerializeField] private string teamALabel = TeamVisualPalette.TeamALabel;
    [SerializeField] private string teamBLabel = TeamVisualPalette.TeamBLabel;
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBColor;
    [SerializeField] private Color neutralColor = Color.white;

    public void UpdateView(
        GameManager.MatchMode matchMode,
        GameManager.MatchState state,
        float remainingSeconds,
        float teamACoverage,
        float teamBCoverage,
        Team winningTeam,
        float playerInkPercent,
        bool playerOnOwnPaint,
        bool playerHasEnoughInk,
        string playerToolLabel,
        bool playerSwimming,
        bool playerWantsToSwim,
        bool playerOnEnemyPaint,
        float playerHealthPercent,
        bool playerEliminated,
        float playerSpecialPercent,
        bool playerSpecialReady,
        Team towerOwner,
        bool towerContested,
        Team towerLeadingTeam,
        float towerProgressPercent,
        float towerTeamAPercent,
        float towerTeamBPercent)
    {
        EnsureRuntimeTextReferences();
        teamAColor = TeamVisualPalette.GetColor(Team.TeamA);
        teamBColor = TeamVisualPalette.GetColor(Team.TeamB);
        Color warningColor = new Color(1f, 0.32f, 0.18f, 1f);
        Color specialColor = playerSpecialReady ? new Color(1f, 0.92f, 0.18f, 1f) : teamAColor;

        if (presentationText != null)
        {
            presentationText.text = FormatPresentation(matchMode, state, teamACoverage, teamBCoverage, winningTeam);
            presentationText.color = GetPresentationColor(state, winningTeam);
        }

        if (timerText != null)
        {
            timerText.text = FormatTime(remainingSeconds);
        }

        if (teamAText != null)
        {
            teamAText.text = $"{teamALabel}: {teamACoverage:0.0}%";
            teamAText.color = teamAColor;
        }

        UpdateHorizontalFill(teamACoverageFill, teamACoverageImage, teamACoverage, HudBarWidth, teamAColor);

        if (teamBText != null)
        {
            teamBText.text = $"{teamBLabel}: {teamBCoverage:0.0}%";
            teamBText.color = teamBColor;
        }

        UpdateHorizontalFill(teamBCoverageFill, teamBCoverageImage, teamBCoverage, HudBarWidth, teamBColor);

        if (inkText != null)
        {
            inkText.text = FormatInk(playerInkPercent, playerOnOwnPaint, playerHasEnoughInk, playerToolLabel, playerSwimming, playerWantsToSwim, playerOnEnemyPaint);
            inkText.color = playerOnEnemyPaint ? teamBColor : playerOnOwnPaint || playerSwimming ? teamAColor : neutralColor;
        }

        UpdateHorizontalFill(inkFill, inkImage, playerInkPercent, HudBarWidth, playerHasEnoughInk ? teamAColor : warningColor);

        if (healthText != null)
        {
            healthText.text = FormatHealth(playerHealthPercent, playerEliminated);
            healthText.color = playerEliminated ? teamBColor : neutralColor;
        }

        UpdateHorizontalFill(healthFill, healthImage, playerHealthPercent, HudBarWidth, playerHealthPercent > 35f ? neutralColor : warningColor);

        if (specialText != null)
        {
            specialText.text = FormatSpecial(playerSpecialPercent, playerSpecialReady);
            specialText.color = playerSpecialReady ? specialColor : neutralColor;
        }

        UpdateHorizontalFill(specialFill, specialImage, playerSpecialPercent, HudBarWidth, specialColor);

        if (objectiveText != null)
        {
            objectiveText.text = FormatObjectiveLine(matchMode);
            objectiveText.color = neutralColor;
        }

        if (towerText != null)
        {
            towerText.text = FormatTowerLine(matchMode, towerOwner, towerContested, towerLeadingTeam, towerProgressPercent, towerTeamAPercent, towerTeamBPercent);
            towerText.color = matchMode == GameManager.MatchMode.TowerControl && towerOwner == Team.TeamA ? teamAColor : matchMode == GameManager.MatchMode.TowerControl && towerOwner == Team.TeamB ? teamBColor : neutralColor;
        }

        Color towerColor = towerLeadingTeam == Team.TeamA ? teamAColor : towerLeadingTeam == Team.TeamB ? teamBColor : neutralColor;
        UpdateHorizontalFill(towerProgressFill, towerProgressImage, matchMode == GameManager.MatchMode.TowerControl ? towerProgressPercent : 0f, TowerBarWidth, towerColor);

        if (statusText != null)
        {
            statusText.text = FormatStatus(state, winningTeam);
            statusText.color = neutralColor;
        }

    }

    public static ScoreUI CreateRuntimeScoreUI()
    {
        GameObject canvasObject = new GameObject("ScoreCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject bannerObject = new GameObject("PresentationBanner");
        bannerObject.transform.SetParent(canvasObject.transform, false);
        RectTransform bannerRect = bannerObject.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -24f);
        bannerRect.sizeDelta = new Vector2(880f, 92f);

        Image bannerImage = bannerObject.AddComponent<Image>();
        bannerImage.color = new Color(0.015f, 0.018f, 0.024f, 0.68f);
        bannerImage.raycastTarget = false;

        GameObject panelObject = new GameObject("ScorePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(540f, 468f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.015f, 0.018f, 0.024f, 0.72f);
        panelImage.raycastTarget = false;

        ScoreUI scoreUI = canvasObject.AddComponent<ScoreUI>();
        scoreUI.presentationText = CreateText(bannerObject.transform, "PresentationText", new Vector2(20f, -12f), 28, FontStyle.Bold, new Vector2(840f, 68f), TextAnchor.MiddleCenter);
        scoreUI.timerText = CreateText(panelObject.transform, "TimerText", new Vector2(18f, -14f), 34, FontStyle.Bold, new Vector2(172f, 42f), TextAnchor.UpperLeft);
        scoreUI.statusText = CreateText(panelObject.transform, "StatusText", new Vector2(204f, -20f), 21, FontStyle.Bold, new Vector2(300f, 32f), TextAnchor.UpperRight);
        scoreUI.teamAText = CreateText(panelObject.transform, "TeamAText", new Vector2(18f, -64f), 23, FontStyle.Bold, new Vector2(500f, 28f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "TeamACoverageBar", new Vector2(18f, -96f), new Vector2(HudBarWidth, 18f), TeamVisualPalette.GetColor(Team.TeamA), out scoreUI.teamACoverageFill, out scoreUI.teamACoverageImage);
        scoreUI.teamBText = CreateText(panelObject.transform, "TeamBText", new Vector2(18f, -124f), 23, FontStyle.Bold, new Vector2(500f, 28f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "TeamBCoverageBar", new Vector2(18f, -156f), new Vector2(HudBarWidth, 18f), TeamVisualPalette.GetColor(Team.TeamB), out scoreUI.teamBCoverageFill, out scoreUI.teamBCoverageImage);
        scoreUI.objectiveText = CreateText(panelObject.transform, "ObjectiveText", new Vector2(18f, -196f), 19, FontStyle.Bold, new Vector2(500f, 28f), TextAnchor.UpperLeft);
        scoreUI.towerText = CreateText(panelObject.transform, "TowerText", new Vector2(18f, -226f), 18, FontStyle.Bold, new Vector2(500f, 48f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "TowerProgressBar", new Vector2(18f, -278f), new Vector2(TowerBarWidth, 14f), Color.white, out scoreUI.towerProgressFill, out scoreUI.towerProgressImage);
        scoreUI.inkText = CreateText(panelObject.transform, "InkText", new Vector2(18f, -310f), 19, FontStyle.Bold, new Vector2(500f, 26f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "InkBar", new Vector2(18f, -336f), new Vector2(HudBarWidth, 14f), TeamVisualPalette.GetColor(Team.TeamA), out scoreUI.inkFill, out scoreUI.inkImage);
        scoreUI.healthText = CreateText(panelObject.transform, "HealthText", new Vector2(18f, -362f), 19, FontStyle.Bold, new Vector2(500f, 26f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "HealthBar", new Vector2(18f, -388f), new Vector2(HudBarWidth, 14f), Color.white, out scoreUI.healthFill, out scoreUI.healthImage);
        scoreUI.specialText = CreateText(panelObject.transform, "SpecialText", new Vector2(18f, -414f), 19, FontStyle.Bold, new Vector2(500f, 26f), TextAnchor.UpperLeft);
        CreateBar(panelObject.transform, "SpecialBar", new Vector2(18f, -438f), new Vector2(HudBarWidth, 14f), new Color(1f, 0.92f, 0.18f, 1f), out scoreUI.specialFill, out scoreUI.specialImage);

        return scoreUI;
    }

    private void Awake()
    {
        EnsureRuntimeTextReferences();
    }

    private void EnsureRuntimeTextReferences()
    {
        if (presentationText != null && timerText != null && teamAText != null && teamBText != null && inkText != null && healthText != null && specialText != null && objectiveText != null && towerText != null && statusText != null && teamACoverageFill != null && teamBCoverageFill != null && towerProgressFill != null && inkFill != null && healthFill != null && specialFill != null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];

            if (text.name == "PresentationText")
            {
                presentationText = text;
            }
            else if (text.name == "TimerText")
            {
                timerText = text;
            }
            else if (text.name == "TeamAText")
            {
                teamAText = text;
            }
            else if (text.name == "TeamBText")
            {
                teamBText = text;
            }
            else if (text.name == "InkText")
            {
                inkText = text;
            }
            else if (text.name == "HealthText")
            {
                healthText = text;
            }
            else if (text.name == "SpecialText")
            {
                specialText = text;
            }
            else if (text.name == "ObjectiveText")
            {
                objectiveText = text;
            }
            else if (text.name == "TowerText")
            {
                towerText = text;
            }
            else if (text.name == "StatusText")
            {
                statusText = text;
            }
        }

        RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];

            if (rect.name == "TeamACoverageBarFill")
            {
                teamACoverageFill = rect;
                teamACoverageImage = rect.GetComponent<Image>();
            }
            else if (rect.name == "TeamBCoverageBarFill")
            {
                teamBCoverageFill = rect;
                teamBCoverageImage = rect.GetComponent<Image>();
            }
            else if (rect.name == "TowerProgressBarFill")
            {
                towerProgressFill = rect;
                towerProgressImage = rect.GetComponent<Image>();
            }
            else if (rect.name == "InkBarFill")
            {
                inkFill = rect;
                inkImage = rect.GetComponent<Image>();
            }
            else if (rect.name == "HealthBarFill")
            {
                healthFill = rect;
                healthImage = rect.GetComponent<Image>();
            }
            else if (rect.name == "SpecialBarFill")
            {
                specialFill = rect;
                specialImage = rect.GetComponent<Image>();
            }
        }
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle)
    {
        return CreateText(parent, name, anchoredPosition, fontSize, fontStyle, new Vector2(320f, 32f), TextAnchor.UpperLeft);
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, int fontSize, FontStyle fontStyle, Vector2 size, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static void CreateBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor, out RectTransform fillRect, out Image fillImage)
    {
        GameObject trackObject = new GameObject(name);
        trackObject.transform.SetParent(parent, false);

        RectTransform trackRect = trackObject.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 1f);
        trackRect.anchorMax = new Vector2(0f, 1f);
        trackRect.pivot = new Vector2(0f, 1f);
        trackRect.anchoredPosition = anchoredPosition;
        trackRect.sizeDelta = size;

        Image trackImage = trackObject.AddComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.12f);
        trackImage.raycastTarget = false;

        GameObject fillObject = new GameObject($"{name}Fill");
        fillObject.transform.SetParent(trackObject.transform, false);

        fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);

        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;
    }

    private static void UpdateHorizontalFill(RectTransform fill, Image image, float percent, float fallbackWidth, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }

        if (fill == null)
        {
            return;
        }

        float normalized = percent < 0f ? 0f : Mathf.Clamp01(percent / 100f);
        RectTransform track = fill.parent as RectTransform;
        float width = track != null && track.rect.width > 0.01f ? track.rect.width : fallbackWidth;
        fill.sizeDelta = new Vector2(width * normalized, 0f);
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static string FormatTime(float seconds)
    {
        int wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = wholeSeconds / 60;
        int remaining = wholeSeconds % 60;
        return $"{minutes:00}:{remaining:00}";
    }

    private string FormatPresentation(GameManager.MatchMode matchMode, GameManager.MatchState state, float teamACoverage, float teamBCoverage, Team winningTeam)
    {
        string modeLabel = FormatMatchMode(matchMode);

        switch (state)
        {
            case GameManager.MatchState.Playing:
                return $"{teamALabel} vs {teamBLabel} | {modeLabel}";
            case GameManager.MatchState.Paused:
                return $"Paused | {modeLabel}";
            case GameManager.MatchState.Finished:
                string winner = winningTeam == Team.None ? "Draw" : $"{GetTeamLabel(winningTeam)} wins";
                return $"{winner} | {modeLabel} | Final {teamALabel} {teamACoverage:0.0}% - {teamBLabel} {teamBCoverage:0.0}%";
            default:
                return $"{teamALabel} vs {teamBLabel} | {modeLabel} | Press Enter";
        }
    }

    private Color GetPresentationColor(GameManager.MatchState state, Team winningTeam)
    {
        if (state == GameManager.MatchState.Finished)
        {
            if (winningTeam == Team.TeamA)
            {
                return teamAColor;
            }

            if (winningTeam == Team.TeamB)
            {
                return teamBColor;
            }
        }

        return neutralColor;
    }

    private string FormatStatus(GameManager.MatchState state, Team winningTeam)
    {
        switch (state)
        {
            case GameManager.MatchState.Playing:
                return "Match in progress";
            case GameManager.MatchState.Paused:
                return "Paused";
            case GameManager.MatchState.Finished:
                return winningTeam == Team.None ? "Draw - press R to restart" : $"{GetTeamLabel(winningTeam)} wins - press R to restart";
            default:
                return "Ready";
        }
    }

    private static string FormatInk(
        float playerInkPercent,
        bool playerOnOwnPaint,
        bool playerHasEnoughInk,
        string playerToolLabel,
        bool playerSwimming,
        bool playerWantsToSwim,
        bool playerOnEnemyPaint)
    {
        if (playerInkPercent < 0f)
        {
            return "Ink: --";
        }

        string stateLabel = playerHasEnoughInk ? "Ready" : "Low";

        if (playerSwimming)
        {
            stateLabel = "Swim";
        }
        else if (playerOnEnemyPaint)
        {
            stateLabel = "Enemy ink";
        }
        else if (playerOnOwnPaint)
        {
            stateLabel = "Refill";
        }
        else if (playerWantsToSwim)
        {
            stateLabel = "Paint needed";
        }

        string toolLabel = string.IsNullOrEmpty(playerToolLabel) ? "Shooter" : playerToolLabel;
        return $"Tool: {toolLabel} | Ink: {Mathf.Clamp(playerInkPercent, 0f, 100f):0}% | {stateLabel}";
    }

    private static string FormatHealth(float playerHealthPercent, bool playerEliminated)
    {
        if (playerHealthPercent < 0f)
        {
            return "HP: --";
        }

        if (playerEliminated)
        {
            return "HP: Respawning";
        }

        return $"HP: {Mathf.Clamp(playerHealthPercent, 0f, 100f):0}%";
    }

    private static string FormatSpecial(float playerSpecialPercent, bool playerSpecialReady)
    {
        if (playerSpecialPercent < 0f)
        {
            return "Special: --";
        }

        if (playerSpecialReady)
        {
            return "Special: Ready";
        }

        return $"Special: {Mathf.Clamp(playerSpecialPercent, 0f, 100f):0}%";
    }

    private static string FormatObjectiveLine(GameManager.MatchMode matchMode)
    {
        if (matchMode == GameManager.MatchMode.TowerControl)
        {
            return "Mode: Tower Control | Push the tower to win";
        }

        return "Mode: Turf War | Total paint wins";
    }

    private string FormatTower(Team towerOwner, bool towerContested, Team towerLeadingTeam, float towerProgressPercent, float towerTeamAPercent, float towerTeamBPercent)
    {
        if (towerProgressPercent < 0f || towerTeamAPercent < 0f || towerTeamBPercent < 0f)
        {
            return "Tower: --";
        }

        string control = "Neutral";

        if (towerContested)
        {
            control = "Contested";
        }
        else if (towerOwner == Team.TeamA || towerOwner == Team.TeamB)
        {
            control = $"{GetTeamLabel(towerOwner)} pushing";
        }

        string lead = towerLeadingTeam == Team.TeamA || towerLeadingTeam == Team.TeamB ? GetTeamLabel(towerLeadingTeam) : "Center";
        return $"Tower: {control} | Lead {lead} {towerProgressPercent:0}% | Paint {teamALabel} {towerTeamAPercent:0}% / {teamBLabel} {towerTeamBPercent:0}%";
    }

    private string FormatTowerLine(GameManager.MatchMode matchMode, Team towerOwner, bool towerContested, Team towerLeadingTeam, float towerProgressPercent, float towerTeamAPercent, float towerTeamBPercent)
    {
        if (matchMode == GameManager.MatchMode.TowerControl)
        {
            return FormatTower(towerOwner, towerContested, towerLeadingTeam, towerProgressPercent, towerTeamAPercent, towerTeamBPercent);
        }

        return "Tower: inactive | M switches to Tower Control";
    }

    private static string FormatMatchMode(GameManager.MatchMode matchMode)
    {
        switch (matchMode)
        {
            case GameManager.MatchMode.TowerControl:
                return "Tower Control";
            default:
                return "Turf War";
        }
    }

    private string GetTeamLabel(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return teamALabel;
            case Team.TeamB:
                return teamBLabel;
            default:
                return TeamVisualPalette.GetLabel(team);
        }
    }
}
