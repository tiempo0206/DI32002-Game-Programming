using System;
using UnityEngine;

/// <summary>
/// Owns the local match loop for the MVP: timer, score polling, and match state.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public enum MatchState
    {
        WaitingToStart,
        Playing,
        Finished
    }

    public static GameManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private bool startMatchOnAwake = true;
    [SerializeField] private bool clearPaintOnMatchStart = true;
    [SerializeField, Min(1f)] private float matchDurationSeconds = 180f;
    [SerializeField] private MatchTimer matchTimer = new MatchTimer();

    [Header("References")]
    [SerializeField] private PaintManager paintManager = null;
    [SerializeField] private ScoreUI scoreUI = null;
    [SerializeField] private bool autoCreateScoreUI = true;

    [Header("Score Refresh")]
    [SerializeField, Min(0.02f)] private float scoreRefreshInterval = 0.1f;

    private MatchState currentState = MatchState.WaitingToStart;
    private float nextScoreRefreshTime;
    private float teamACoverage;
    private float teamBCoverage;
    private Team winningTeam = Team.None;

    public event Action<MatchState> MatchStateChanged;

    public MatchState CurrentState => currentState;
    public float RemainingSeconds => matchTimer.RemainingSeconds;
    public float MatchDurationSeconds => matchTimer.DurationSeconds;
    public float TeamACoverage => teamACoverage;
    public float TeamBCoverage => teamBCoverage;
    public Team WinningTeam => winningTeam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one GameManager exists. The latest one will become the active instance.", this);
        }

        Instance = this;
        ResolveReferences();
        matchTimer.Configure(matchDurationSeconds);
        matchTimer.Reset();
    }

    private void Start()
    {
        RefreshScore(true);
        UpdateScoreUI();

        if (startMatchOnAwake)
        {
            StartMatch();
        }
    }

    private void Update()
    {
        if (currentState != MatchState.Playing)
        {
            UpdateScoreUI();
            return;
        }

        if (matchTimer.Tick(Time.deltaTime))
        {
            EndMatch();
            return;
        }

        if (Time.time >= nextScoreRefreshTime)
        {
            RefreshScore(false);
            UpdateScoreUI();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartMatch()
    {
        ResolveReferences();

        if (clearPaintOnMatchStart && paintManager != null)
        {
            paintManager.ClearAllPaint();
        }

        matchTimer.Configure(matchDurationSeconds);
        matchTimer.Reset();
        matchTimer.Start();
        SetState(MatchState.Playing);
        RefreshScore(true);
        UpdateScoreUI();
    }

    public void EndMatch()
    {
        matchTimer.Stop();
        RefreshScore(true);
        winningTeam = GetWinningTeam();
        SetState(MatchState.Finished);
        UpdateScoreUI();
    }

    public void ResetMatch()
    {
        matchTimer.Configure(matchDurationSeconds);
        matchTimer.Reset();

        if (paintManager != null)
        {
            paintManager.ClearAllPaint();
        }

        winningTeam = Team.None;
        SetState(MatchState.WaitingToStart);
        RefreshScore(true);
        UpdateScoreUI();
    }

    public float GetCoveragePercent(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return teamACoverage;
            case Team.TeamB:
                return teamBCoverage;
            default:
                return 0f;
        }
    }

    [ContextMenu("Debug Start Match")]
    private void DebugStartMatch()
    {
        StartMatch();
    }

    [ContextMenu("Debug End Match")]
    private void DebugEndMatch()
    {
        EndMatch();
    }

    [ContextMenu("Debug Reset Match")]
    private void DebugResetMatch()
    {
        ResetMatch();
    }

    private void ResolveReferences()
    {
        if (paintManager == null)
        {
            paintManager = PaintManager.Instance != null ? PaintManager.Instance : FindObjectOfType<PaintManager>();
        }

        if (scoreUI == null)
        {
            scoreUI = FindObjectOfType<ScoreUI>();
        }

        if (scoreUI == null && autoCreateScoreUI)
        {
            scoreUI = ScoreUI.CreateRuntimeScoreUI();
        }
    }

    private void RefreshScore(bool forceNextRefresh)
    {
        if (paintManager == null)
        {
            teamACoverage = 0f;
            teamBCoverage = 0f;
        }
        else
        {
            teamACoverage = paintManager.GetCoveragePercent(Team.TeamA);
            teamBCoverage = paintManager.GetCoveragePercent(Team.TeamB);
        }

        winningTeam = GetWinningTeam();

        if (forceNextRefresh)
        {
            nextScoreRefreshTime = Time.time;
        }
        else
        {
            nextScoreRefreshTime = Time.time + scoreRefreshInterval;
        }
    }

    private Team GetWinningTeam()
    {
        if (Mathf.Approximately(teamACoverage, teamBCoverage))
        {
            return Team.None;
        }

        return teamACoverage > teamBCoverage ? Team.TeamA : Team.TeamB;
    }

    private void UpdateScoreUI()
    {
        if (scoreUI == null)
        {
            return;
        }

        scoreUI.UpdateView(
            currentState,
            matchTimer.RemainingSeconds,
            teamACoverage,
            teamBCoverage,
            winningTeam);
    }

    private void SetState(MatchState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;
        MatchStateChanged?.Invoke(currentState);
    }
}
