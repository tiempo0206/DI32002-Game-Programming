using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD for match timer and team paint coverage.
/// It can be wired in the scene or auto-created by GameManager for quick testing.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private Text timerText = null;
    [SerializeField] private Text teamAText = null;
    [SerializeField] private Text teamBText = null;
    [SerializeField] private Text inkText = null;
    [SerializeField] private Text statusText = null;
    [SerializeField] private Text controlsText = null;

    [Header("Formatting")]
    [SerializeField] private string teamALabel = TeamVisualPalette.TeamALabel;
    [SerializeField] private string teamBLabel = TeamVisualPalette.TeamBLabel;
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBColor;
    [SerializeField] private Color neutralColor = Color.white;

    public void UpdateView(
        GameManager.MatchState state,
        float remainingSeconds,
        float teamACoverage,
        float teamBCoverage,
        Team winningTeam,
        float playerInkPercent,
        bool playerOnOwnPaint,
        bool playerHasEnoughInk)
    {
        EnsureRuntimeTextReferences();

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
            inkText.text = FormatInk(playerInkPercent, playerOnOwnPaint, playerHasEnoughInk);
            inkText.color = playerOnOwnPaint ? teamAColor : neutralColor;
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

        GameObject panelObject = new GameObject("ScorePanel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(420f, 216f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.45f);
        panelImage.raycastTarget = false;

        ScoreUI scoreUI = canvasObject.AddComponent<ScoreUI>();
        scoreUI.timerText = CreateText(panelObject.transform, "TimerText", new Vector2(16f, -12f), 32, FontStyle.Bold);
        scoreUI.teamAText = CreateText(panelObject.transform, "TeamAText", new Vector2(16f, -58f), 24, FontStyle.Bold);
        scoreUI.teamBText = CreateText(panelObject.transform, "TeamBText", new Vector2(16f, -90f), 24, FontStyle.Bold);
        scoreUI.inkText = CreateText(panelObject.transform, "InkText", new Vector2(16f, -122f), 20, FontStyle.Bold);
        scoreUI.statusText = CreateText(panelObject.transform, "StatusText", new Vector2(16f, -154f), 20, FontStyle.Normal);
        scoreUI.controlsText = CreateText(panelObject.transform, "ControlsText", new Vector2(16f, -182f), 18, FontStyle.Normal);

        return scoreUI;
    }

    private void Awake()
    {
        EnsureRuntimeTextReferences();
    }

    private void EnsureRuntimeTextReferences()
    {
        if (timerText != null && teamAText != null && teamBText != null && inkText != null && statusText != null && controlsText != null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];

            if (text.name == "TimerText")
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
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(320f, 32f);

        Text text = textObject.AddComponent<Text>();
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.UpperLeft;
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
                return "R Restart | P Pause";
            case GameManager.MatchState.Paused:
                return "R Restart | P Resume";
            case GameManager.MatchState.Finished:
                return "R Restart";
            default:
                return "Enter Start | R Restart";
        }
    }

    private static string FormatInk(float playerInkPercent, bool playerOnOwnPaint, bool playerHasEnoughInk)
    {
        if (playerInkPercent < 0f)
        {
            return "Ink: --";
        }

        string stateLabel = playerHasEnoughInk ? "Ready" : "Low";

        if (playerOnOwnPaint)
        {
            stateLabel = "Refill";
        }

        return $"Ink: {Mathf.Clamp(playerInkPercent, 0f, 100f):0}% | {stateLabel}";
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
