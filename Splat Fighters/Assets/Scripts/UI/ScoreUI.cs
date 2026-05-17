using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD for match timer and team paint coverage.
/// It can be wired in the scene or auto-created by GameManager for quick testing.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private Text presentationText = null;
    [SerializeField] private Text timerText = null;
    [SerializeField] private Text teamAText = null;
    [SerializeField] private Text teamBText = null;
    [SerializeField] private Text inkText = null;
    [SerializeField] private Text healthText = null;
    [SerializeField] private Text specialText = null;
    [SerializeField] private Text zoneText = null;
    [SerializeField] private Text towerText = null;
    [SerializeField] private Text statusText = null;
    [SerializeField] private Text controlsText = null;

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
        bool playerSwimming,
        bool playerWantsToSwim,
        bool playerOnEnemyPaint,
        float playerHealthPercent,
        bool playerEliminated,
        float playerSpecialPercent,
        bool playerSpecialReady,
        Team zoneOwner,
        bool zoneContested,
        float zoneTeamAPercent,
        float zoneTeamBPercent,
        Team towerOwner,
        bool towerContested,
        Team towerLeadingTeam,
        float towerProgressPercent,
        float towerTeamAPercent,
        float towerTeamBPercent)
    {
        EnsureRuntimeTextReferences();

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

        if (teamBText != null)
        {
            teamBText.text = $"{teamBLabel}: {teamBCoverage:0.0}%";
            teamBText.color = teamBColor;
        }

        if (inkText != null)
        {
            inkText.text = FormatInk(playerInkPercent, playerOnOwnPaint, playerHasEnoughInk, playerSwimming, playerWantsToSwim, playerOnEnemyPaint);
            inkText.color = playerOnEnemyPaint ? teamBColor : playerOnOwnPaint || playerSwimming ? teamAColor : neutralColor;
        }

        if (healthText != null)
        {
            healthText.text = FormatHealth(playerHealthPercent, playerEliminated);
            healthText.color = playerEliminated ? teamBColor : neutralColor;
        }

        if (specialText != null)
        {
            specialText.text = FormatSpecial(playerSpecialPercent, playerSpecialReady);
            specialText.color = playerSpecialReady ? teamAColor : neutralColor;
        }

        if (zoneText != null)
        {
            zoneText.text = FormatObjectiveLine(matchMode, zoneOwner, zoneContested, zoneTeamAPercent, zoneTeamBPercent);
            zoneText.color = matchMode == GameManager.MatchMode.SplatZones && zoneOwner == Team.TeamA ? teamAColor : matchMode == GameManager.MatchMode.SplatZones && zoneOwner == Team.TeamB ? teamBColor : neutralColor;
        }

        if (towerText != null)
        {
            towerText.text = FormatTowerLine(matchMode, towerOwner, towerContested, towerLeadingTeam, towerProgressPercent, towerTeamAPercent, towerTeamBPercent);
            towerText.color = matchMode == GameManager.MatchMode.TowerControl && towerOwner == Team.TeamA ? teamAColor : matchMode == GameManager.MatchMode.TowerControl && towerOwner == Team.TeamB ? teamBColor : neutralColor;
        }

        if (statusText != null)
        {
            statusText.text = FormatStatus(state, winningTeam);
            statusText.color = neutralColor;
        }

        if (controlsText != null)
        {
            controlsText.text = FormatControls(state);
            controlsText.color = neutralColor;
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
        bannerRect.sizeDelta = new Vector2(820f, 96f);

        Image bannerImage = bannerObject.AddComponent<Image>();
        bannerImage.color = new Color(0f, 0f, 0f, 0.5f);
        bannerImage.raycastTarget = false;

        GameObject panelObject = new GameObject("ScorePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(500f, 336f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);
        panelImage.raycastTarget = false;

        ScoreUI scoreUI = canvasObject.AddComponent<ScoreUI>();
        scoreUI.presentationText = CreateText(bannerObject.transform, "PresentationText", new Vector2(16f, -14f), 28, FontStyle.Bold, new Vector2(788f, 68f), TextAnchor.MiddleCenter);
        scoreUI.timerText = CreateText(panelObject.transform, "TimerText", new Vector2(16f, -12f), 32, FontStyle.Bold);
        scoreUI.teamAText = CreateText(panelObject.transform, "TeamAText", new Vector2(16f, -58f), 24, FontStyle.Bold);
        scoreUI.teamBText = CreateText(panelObject.transform, "TeamBText", new Vector2(16f, -90f), 24, FontStyle.Bold);
        scoreUI.inkText = CreateText(panelObject.transform, "InkText", new Vector2(16f, -122f), 20, FontStyle.Bold);
        scoreUI.healthText = CreateText(panelObject.transform, "HealthText", new Vector2(16f, -150f), 20, FontStyle.Bold);
        scoreUI.specialText = CreateText(panelObject.transform, "SpecialText", new Vector2(16f, -178f), 20, FontStyle.Bold);
        scoreUI.zoneText = CreateText(panelObject.transform, "ZoneText", new Vector2(16f, -206f), 19, FontStyle.Bold, new Vector2(450f, 32f), TextAnchor.UpperLeft);
        scoreUI.towerText = CreateText(panelObject.transform, "TowerText", new Vector2(16f, -234f), 19, FontStyle.Bold, new Vector2(450f, 32f), TextAnchor.UpperLeft);
        scoreUI.statusText = CreateText(panelObject.transform, "StatusText", new Vector2(16f, -264f), 20, FontStyle.Normal);
        scoreUI.controlsText = CreateText(panelObject.transform, "ControlsText", new Vector2(16f, -292f), 18, FontStyle.Normal, new Vector2(450f, 32f), TextAnchor.UpperLeft);

        return scoreUI;
    }

    private void Awake()
    {
        EnsureRuntimeTextReferences();
    }

    private void EnsureRuntimeTextReferences()
    {
        if (presentationText != null && timerText != null && teamAText != null && teamBText != null && inkText != null && healthText != null && specialText != null && zoneText != null && towerText != null && statusText != null && controlsText != null)
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
            else if (text.name == "ZoneText")
            {
                zoneText = text;
            }
            else if (text.name == "TowerText")
            {
                towerText = text;
            }
            else if (text.name == "StatusText")
            {
                statusText = text;
            }
            else if (text.name == "ControlsText")
            {
                controlsText = text;
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
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

    private static string FormatControls(GameManager.MatchState state)
    {
        switch (state)
        {
            case GameManager.MatchState.Playing:
                return "Shift Swim | Q Special | M Mode | R Restart | P Pause";
            case GameManager.MatchState.Paused:
                return "M Mode | R Restart | P Resume";
            case GameManager.MatchState.Finished:
                return "M Mode | R Restart";
            default:
                return "Enter Start | M Mode | R Restart";
        }
    }

    private static string FormatInk(
        float playerInkPercent,
        bool playerOnOwnPaint,
        bool playerHasEnoughInk,
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

        return $"Ink: {Mathf.Clamp(playerInkPercent, 0f, 100f):0}% | {stateLabel}";
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

    private string FormatZone(Team zoneOwner, bool zoneContested, float zoneTeamAPercent, float zoneTeamBPercent)
    {
        if (zoneTeamAPercent < 0f || zoneTeamBPercent < 0f)
        {
            return "Zone: --";
        }

        if (zoneContested)
        {
            return $"Zone: Contested | {teamALabel} {zoneTeamAPercent:0}% / {teamBLabel} {zoneTeamBPercent:0}%";
        }

        if (zoneOwner == Team.TeamA || zoneOwner == Team.TeamB)
        {
            return $"Zone: {GetTeamLabel(zoneOwner)} controls | {teamALabel} {zoneTeamAPercent:0}% / {teamBLabel} {zoneTeamBPercent:0}%";
        }

        return $"Zone: Neutral | {teamALabel} {zoneTeamAPercent:0}% / {teamBLabel} {zoneTeamBPercent:0}%";
    }

    private string FormatObjectiveLine(GameManager.MatchMode matchMode, Team zoneOwner, bool zoneContested, float zoneTeamAPercent, float zoneTeamBPercent)
    {
        switch (matchMode)
        {
            case GameManager.MatchMode.SplatZones:
                return FormatZone(zoneOwner, zoneContested, zoneTeamAPercent, zoneTeamBPercent);
            case GameManager.MatchMode.TowerControl:
                return "Mode: Tower Control | Paint pushes tower";
            default:
                return "Mode: Turf War | Total paint wins";
        }
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
        return $"Tower: {control} | {lead} {towerProgressPercent:0}%";
    }

    private string FormatTowerLine(GameManager.MatchMode matchMode, Team towerOwner, bool towerContested, Team towerLeadingTeam, float towerProgressPercent, float towerTeamAPercent, float towerTeamBPercent)
    {
        if (matchMode == GameManager.MatchMode.TowerControl)
        {
            return FormatTower(towerOwner, towerContested, towerLeadingTeam, towerProgressPercent, towerTeamAPercent, towerTeamBPercent);
        }

        if (matchMode == GameManager.MatchMode.SplatZones)
        {
            return "Tower: inactive | Zone is active";
        }

        return "Objectives: M cycles demo mode";
    }

    private static string FormatMatchMode(GameManager.MatchMode matchMode)
    {
        switch (matchMode)
        {
            case GameManager.MatchMode.SplatZones:
                return "Splat Zones";
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
