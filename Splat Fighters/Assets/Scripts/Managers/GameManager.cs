using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the local match loop for the MVP: timer, score polling, and match state.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    private const string MatchModePrefKey = "SplatFighters.Menu.MatchMode";

    public enum MatchState
    {
        WaitingToStart,
        Playing,
        Paused,
        Finished
    }

    public enum MatchMode
    {
        TurfWar,
        SplatZones,
        TowerControl
    }

    public static GameManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private bool startMatchOnAwake = true;
    [SerializeField] private bool clearPaintOnMatchStart = true;
    [SerializeField] private bool resetCharactersOnMatchStart = true;
    [SerializeField] private bool destroyProjectilesOnMatchStart = true;
    [SerializeField] private MatchMode matchMode = MatchMode.TurfWar;
    [SerializeField, Min(1f)] private float matchDurationSeconds = 180f;
    [SerializeField] private MatchTimer matchTimer = new MatchTimer();

    [Header("References")]
    [SerializeField] private PaintManager paintManager = null;
    [SerializeField] private ScoreUI scoreUI = null;
    [SerializeField] private MatchResultsUI resultsUI = null;
    [SerializeField] private Transform playerRoot = null;
    [SerializeField] private PlayerController playerController = null;
    [SerializeField] private CharacterHealth playerHealth = null;
    [SerializeField] private InkWeapon playerWeapon = null;
    [SerializeField] private PlayerToolSwitcher playerToolSwitcher = null;
    [SerializeField] private SpecialMeter playerSpecialMeter = null;
    [SerializeField] private SplatZoneObjective centerZoneObjective = null;
    [SerializeField] private TowerObjective centerTowerObjective = null;
    [SerializeField] private BotController teamBBot = null;
    [SerializeField] private CharacterHealth teamBBotHealth = null;
    [SerializeField] private SpawnPoint teamASpawn = null;
    [SerializeField] private SpawnPoint teamBSpawn = null;
    [SerializeField] private bool autoCreateScoreUI = true;
    [SerializeField] private bool autoCreateResultsUI = true;

    [Header("Respawn")]
    [SerializeField, Min(0f)] private float respawnDelaySeconds = 2f;

    [Header("Score Refresh")]
    [SerializeField, Min(0.02f)] private float scoreRefreshInterval = 0.2f;

    [Header("Quick Controls")]
    [SerializeField] private bool enableKeyboardControls = true;
    [SerializeField] private KeyCode startKey = KeyCode.Return;
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private KeyCode alternatePauseKey = KeyCode.Escape;
    [SerializeField] private KeyCode cycleModeKey = KeyCode.M;
    [SerializeField] private bool pauseUsesTimeScale = true;

    private MatchState currentState = MatchState.WaitingToStart;
    private float nextScoreRefreshTime;
    private float teamACoverage;
    private float teamBCoverage;
    private Team winningTeam = Team.None;
    private float timeScaleBeforePause = 1f;
    private bool timeScaleOverridden;
    private Coroutine playerRespawnRoutine;
    private Coroutine teamBBotRespawnRoutine;

    public event Action<MatchState> MatchStateChanged;

    public MatchState CurrentState => currentState;
    public MatchMode CurrentMatchMode => matchMode;
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
        LoadMatchModeFromPreferences();
        ResolveReferences();
        SubscribeToHealthEvents();
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
        HandleKeyboardControls();

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
            UnsubscribeFromHealthEvents();
            ApplyPausedTimeScale(false);
            Instance = null;
        }
    }

    public void StartMatch()
    {
        ResolveReferences();
        SubscribeToHealthEvents();
        StopRespawnRoutines();

        if (clearPaintOnMatchStart && paintManager != null)
        {
            paintManager.ClearAllPaint();
        }

        if (destroyProjectilesOnMatchStart)
        {
            DestroyActiveProjectiles();
        }

        ResetObjectives();

        if (resetCharactersOnMatchStart)
        {
            ResetCharactersToSpawns();
        }

        ApplyPausedTimeScale(false);
        matchTimer.Configure(matchDurationSeconds);
        matchTimer.Reset();
        matchTimer.Start();
        SetState(MatchState.Playing);
        SplatAudioManager.PlayMatchStartSound();
        RefreshScore(true);
        UpdateScoreUI();
    }

    public void EndMatch()
    {
        StopRespawnRoutines();
        ApplyPausedTimeScale(false);
        matchTimer.Stop();
        RefreshScore(true);
        winningTeam = GetWinningTeam();
        SetState(MatchState.Finished);
        SplatAudioManager.PlayMatchEndSound();
        UpdateScoreUI();
    }

    public void ResetMatch()
    {
        ApplyPausedTimeScale(false);
        StopRespawnRoutines();
        matchTimer.Configure(matchDurationSeconds);
        matchTimer.Reset();

        if (paintManager != null)
        {
            paintManager.ClearAllPaint();
        }

        if (destroyProjectilesOnMatchStart)
        {
            DestroyActiveProjectiles();
        }

        ResetObjectives();

        if (resetCharactersOnMatchStart)
        {
            ResetCharactersToSpawns();
        }

        winningTeam = Team.None;
        SetState(MatchState.WaitingToStart);
        RefreshScore(true);
        UpdateScoreUI();
    }

    public void RestartMatch()
    {
        StartMatch();
    }

    public void PauseMatch()
    {
        if (currentState != MatchState.Playing)
        {
            return;
        }

        matchTimer.Stop();
        ApplyPausedTimeScale(true);
        SetState(MatchState.Paused);
        RefreshScore(true);
        UpdateScoreUI();
    }

    public void ResumeMatch()
    {
        if (currentState != MatchState.Paused)
        {
            return;
        }

        ApplyPausedTimeScale(false);
        matchTimer.Start();
        SetState(MatchState.Playing);
        RefreshScore(true);
        UpdateScoreUI();
    }

    public void TogglePause()
    {
        if (currentState == MatchState.Playing)
        {
            PauseMatch();
        }
        else if (currentState == MatchState.Paused)
        {
            ResumeMatch();
        }
    }

    public void CycleMatchMode()
    {
        matchMode = matchMode == MatchMode.TowerControl ? MatchMode.TurfWar : (MatchMode)((int)matchMode + 1);
        UpdateScoreUI();
    }

    public void SetMatchMode(MatchMode newMode)
    {
        matchMode = newMode;
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

    [ContextMenu("Debug Restart Match")]
    private void DebugRestartMatch()
    {
        RestartMatch();
    }

    [ContextMenu("Debug Toggle Pause")]
    private void DebugTogglePause()
    {
        TogglePause();
    }

    [ContextMenu("Debug Cycle Match Mode")]
    private void DebugCycleMatchMode()
    {
        CycleMatchMode();
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

        if (resultsUI == null)
        {
            resultsUI = FindObjectOfType<MatchResultsUI>();
        }

        if (resultsUI == null && autoCreateResultsUI)
        {
            resultsUI = MatchResultsUI.CreateRuntimeResultsUI();
        }

        if (resultsUI != null)
        {
            resultsUI.Bind(this);
        }

        if (playerRoot == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            playerRoot = playerObject != null ? playerObject.transform : null;
        }

        if (playerController == null && playerRoot != null)
        {
            playerController = playerRoot.GetComponent<PlayerController>();
        }

        if (playerHealth == null && playerRoot != null)
        {
            playerHealth = playerRoot.GetComponent<CharacterHealth>();
        }

        if (playerWeapon == null && playerRoot != null)
        {
            playerWeapon = playerRoot.GetComponentInChildren<InkWeapon>();
        }

        if (playerToolSwitcher == null && playerRoot != null)
        {
            playerToolSwitcher = playerRoot.GetComponentInChildren<PlayerToolSwitcher>();
        }

        if (playerSpecialMeter == null && playerRoot != null)
        {
            playerSpecialMeter = playerRoot.GetComponentInChildren<SpecialMeter>();
        }

        if (centerZoneObjective == null)
        {
            centerZoneObjective = FindObjectOfType<SplatZoneObjective>();
        }

        if (centerTowerObjective == null)
        {
            centerTowerObjective = FindObjectOfType<TowerObjective>();
        }

        if (teamBBot == null)
        {
            teamBBot = FindObjectOfType<BotController>();
        }

        if (teamBBotHealth == null && teamBBot != null)
        {
            teamBBotHealth = teamBBot.GetComponent<CharacterHealth>();
        }

        if (teamASpawn == null || teamBSpawn == null)
        {
            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnPoint spawnPoint = spawnPoints[i];

                if (spawnPoint == null || !spawnPoint.DefaultForTeam)
                {
                    continue;
                }

                if (spawnPoint.Team == Team.TeamA && teamASpawn == null)
                {
                    teamASpawn = spawnPoint;
                }
                else if (spawnPoint.Team == Team.TeamB && teamBSpawn == null)
                {
                    teamBSpawn = spawnPoint;
                }
            }
        }
    }

    private void HandleKeyboardControls()
    {
        if (!enableKeyboardControls)
        {
            return;
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartMatch();
            return;
        }

        if (Input.GetKeyDown(startKey) && currentState == MatchState.WaitingToStart)
        {
            StartMatch();
            return;
        }

        if (Input.GetKeyDown(cycleModeKey))
        {
            CycleMatchMode();
            return;
        }

        if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(alternatePauseKey))
        {
            TogglePause();
        }
    }

    private void ResetCharactersToSpawns()
    {
        ResolveReferences();

        TeleportCharacter(playerRoot, teamASpawn);

        if (playerHealth != null)
        {
            playerHealth.ReviveFull();
        }

        if (playerController != null)
        {
            playerController.ResetMotionState();
        }

        ResetWeaponResources(playerRoot);
        ResetSpecialMeters(playerRoot);

        if (teamBBot != null)
        {
            TeleportCharacter(teamBBot.transform, teamBSpawn);

            if (teamBBotHealth != null)
            {
                teamBBotHealth.ReviveFull();
            }

            teamBBot.ResetBotState();
            ResetWeaponResources(teamBBot.transform);
            ResetSpecialMeters(teamBBot.transform);
        }
    }

    private void TeleportCharacter(Transform characterRoot, SpawnPoint spawnPoint)
    {
        if (characterRoot == null || spawnPoint == null)
        {
            return;
        }

        CharacterController characterController = characterRoot.GetComponent<CharacterController>();
        bool restoreController = characterController != null && characterController.enabled;

        if (restoreController)
        {
            characterController.enabled = false;
        }

        characterRoot.SetPositionAndRotation(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);

        if (restoreController)
        {
            characterController.enabled = true;
        }
    }

    private void DestroyActiveProjectiles()
    {
        InkProjectile[] projectiles = FindObjectsOfType<InkProjectile>();

        for (int i = 0; i < projectiles.Length; i++)
        {
            InkProjectile projectile = projectiles[i];

            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
    }

    private void ResetWeaponResources(Transform root)
    {
        if (root == null)
        {
            return;
        }

        InkWeapon[] weapons = root.GetComponentsInChildren<InkWeapon>();

        for (int i = 0; i < weapons.Length; i++)
        {
            InkWeapon weapon = weapons[i];

            if (weapon != null)
            {
                weapon.ResetInkResource();
            }
        }
    }

    private void ResetSpecialMeters(Transform root)
    {
        if (root == null)
        {
            return;
        }

        SpecialMeter[] specialMeters = root.GetComponentsInChildren<SpecialMeter>();

        for (int i = 0; i < specialMeters.Length; i++)
        {
            SpecialMeter meter = specialMeters[i];

            if (meter != null)
            {
                meter.ResetCharge();
            }
        }
    }

    private void ResetObjectives()
    {
        if (centerTowerObjective != null)
        {
            centerTowerObjective.ResetObjective();
        }
    }

    private void SubscribeToHealthEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.Eliminated -= HandleCharacterEliminated;
            playerHealth.Eliminated += HandleCharacterEliminated;
        }

        if (teamBBotHealth != null)
        {
            teamBBotHealth.Eliminated -= HandleCharacterEliminated;
            teamBBotHealth.Eliminated += HandleCharacterEliminated;
        }
    }

    private void UnsubscribeFromHealthEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.Eliminated -= HandleCharacterEliminated;
        }

        if (teamBBotHealth != null)
        {
            teamBBotHealth.Eliminated -= HandleCharacterEliminated;
        }
    }

    private void HandleCharacterEliminated(CharacterHealth health)
    {
        if (health == null || currentState != MatchState.Playing)
        {
            return;
        }

        if (health == playerHealth && playerRespawnRoutine == null)
        {
            playerRespawnRoutine = StartCoroutine(RespawnCharacterAfterDelay(playerHealth, playerRoot, teamASpawn, true));
        }
        else if (health == teamBBotHealth && teamBBot != null && teamBBotRespawnRoutine == null)
        {
            teamBBotRespawnRoutine = StartCoroutine(RespawnCharacterAfterDelay(teamBBotHealth, teamBBot.transform, teamBSpawn, false));
        }
    }

    private IEnumerator RespawnCharacterAfterDelay(CharacterHealth health, Transform root, SpawnPoint spawnPoint, bool isPlayer)
    {
        float elapsed = 0f;

        while (elapsed < respawnDelaySeconds)
        {
            if (currentState == MatchState.WaitingToStart || currentState == MatchState.Finished)
            {
                ClearRespawnRoutineReference(isPlayer);
                yield break;
            }

            if (currentState == MatchState.Playing)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        if (currentState == MatchState.Playing)
        {
            TeleportCharacter(root, spawnPoint);

            if (health != null)
            {
                health.ReviveFull();
            }

            if (isPlayer && playerController != null)
            {
                playerController.ResetMotionState();
            }
            else if (!isPlayer && teamBBot != null)
            {
                teamBBot.ResetBotState();
            }

            ResetWeaponResources(root);
        }

        ClearRespawnRoutineReference(isPlayer);
    }

    private void StopRespawnRoutines()
    {
        if (playerRespawnRoutine != null)
        {
            StopCoroutine(playerRespawnRoutine);
            playerRespawnRoutine = null;
        }

        if (teamBBotRespawnRoutine != null)
        {
            StopCoroutine(teamBBotRespawnRoutine);
            teamBBotRespawnRoutine = null;
        }
    }

    private void ClearRespawnRoutineReference(bool isPlayer)
    {
        if (isPlayer)
        {
            playerRespawnRoutine = null;
        }
        else
        {
            teamBBotRespawnRoutine = null;
        }
    }

    private void ApplyPausedTimeScale(bool paused)
    {
        if (!pauseUsesTimeScale)
        {
            return;
        }

        if (paused)
        {
            if (!timeScaleOverridden)
            {
                timeScaleBeforePause = Time.timeScale;
                timeScaleOverridden = true;
            }

            Time.timeScale = 0f;
            return;
        }

        if (timeScaleOverridden)
        {
            Time.timeScale = timeScaleBeforePause;
            timeScaleOverridden = false;
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
            matchMode,
            currentState,
            matchTimer.RemainingSeconds,
            teamACoverage,
            teamBCoverage,
            winningTeam,
            playerWeapon != null ? playerWeapon.InkPercent : -1f,
            playerWeapon != null && playerWeapon.IsReceivingOwnPaintRecovery,
            playerWeapon == null || playerWeapon.HasEnoughInkToFire,
            playerToolSwitcher != null ? playerToolSwitcher.CurrentToolLabel : "Shooter",
            playerController != null && playerController.IsSwimming,
            playerController != null && playerController.WantsToSwim,
            playerController != null && playerController.IsOnEnemyPaint,
            playerHealth != null ? playerHealth.HealthPercent : -1f,
            playerHealth != null && playerHealth.IsEliminated,
            playerSpecialMeter != null ? playerSpecialMeter.ChargePercent : -1f,
            playerSpecialMeter != null && playerSpecialMeter.IsReady,
            centerZoneObjective != null ? centerZoneObjective.ControllingTeam : Team.None,
            centerZoneObjective != null && centerZoneObjective.IsContested,
            centerZoneObjective != null ? centerZoneObjective.TeamAPercent : -1f,
            centerZoneObjective != null ? centerZoneObjective.TeamBPercent : -1f,
            centerTowerObjective != null ? centerTowerObjective.ControllingTeam : Team.None,
            centerTowerObjective != null && centerTowerObjective.IsContested,
            centerTowerObjective != null ? centerTowerObjective.LeadingTeam : Team.None,
            centerTowerObjective != null ? centerTowerObjective.RouteProgressPercent : -1f,
            centerTowerObjective != null ? centerTowerObjective.TeamAPercent : -1f,
            centerTowerObjective != null ? centerTowerObjective.TeamBPercent : -1f);
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

    private void LoadMatchModeFromPreferences()
    {
        if (!PlayerPrefs.HasKey(MatchModePrefKey))
        {
            return;
        }

        int rawMode = PlayerPrefs.GetInt(MatchModePrefKey, (int)matchMode);

        if (rawMode < (int)MatchMode.TurfWar || rawMode > (int)MatchMode.TowerControl)
        {
            return;
        }

        matchMode = (MatchMode)rawMode;
    }
}
