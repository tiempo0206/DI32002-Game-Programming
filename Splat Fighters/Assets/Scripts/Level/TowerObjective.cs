using UnityEngine;

/// <summary>
/// Paint-driven Tower Control-style objective for the local MVP.
/// The tower moves along an authored route when one team owns enough nearby paint.
/// </summary>
[DisallowMultipleComponent]
public class TowerObjective : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private Transform teamBGoal = null;
    [SerializeField] private Transform centerPoint = null;
    [SerializeField] private Transform teamAGoal = null;
    [SerializeField, Range(-1f, 1f)] private float routeProgress = 0f;
    [SerializeField, Min(0f)] private float moveSpeed = 0.22f;

    [Header("Control Bounds")]
    [SerializeField] private Vector2 controlSize = new Vector2(3.2f, 2.6f);
    [SerializeField, Min(0.01f)] private float controlHeight = 0.5f;
    [SerializeField, Range(0f, 100f)] private float controlThresholdPercent = 52f;
    [SerializeField, Range(0f, 100f)] private float minimumPaintedPercent = 18f;
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.25f;

    [Header("Visuals")]
    [SerializeField] private Renderer towerRenderer = null;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color contestedColor = new Color(1f, 0.95f, 0.2f, 1f);
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBColor;

    private float nextRefreshTime;
    private Team controllingTeam = Team.None;
    private bool contested;
    private float teamAPercent;
    private float teamBPercent;
    private float paintedPercent;

    public Team ControllingTeam => controllingTeam;
    public bool IsContested => contested;
    public Team LeadingTeam => routeProgress > 0.01f ? Team.TeamA : routeProgress < -0.01f ? Team.TeamB : Team.None;
    public float RouteProgressPercent => Mathf.Abs(routeProgress) * 100f;
    public float TeamAPercent => teamAPercent;
    public float TeamBPercent => teamBPercent;
    public float PaintedPercent => paintedPercent;

    private void Awake()
    {
        ResolveReferences();
        ApplyRoutePosition();
        RefreshControlState();
    }

    private void Update()
    {
        if (Time.time >= nextRefreshTime)
        {
            RefreshControlState();
        }

        UpdateRouteProgress(Time.deltaTime);
    }

    public void ResetObjective()
    {
        routeProgress = 0f;
        controllingTeam = Team.None;
        contested = false;
        teamAPercent = 0f;
        teamBPercent = 0f;
        paintedPercent = 0f;
        ApplyRoutePosition();
        ApplyTowerColor();
        nextRefreshTime = Time.time;
    }

    public void RefreshControlState()
    {
        nextRefreshTime = Time.time + refreshInterval;

        if (PaintManager.Instance == null || !PaintManager.Instance.GetTeamCellCountsInWorldBounds(GetControlBounds(), out int totalCells, out int teamACells, out int teamBCells))
        {
            SetControlState(Team.None, false, 0f, 0f, 0f);
            return;
        }

        float nextTeamAPercent = teamACells * 100f / totalCells;
        float nextTeamBPercent = teamBCells * 100f / totalCells;
        float nextPaintedPercent = (teamACells + teamBCells) * 100f / totalCells;

        if (nextPaintedPercent < minimumPaintedPercent)
        {
            SetControlState(Team.None, false, nextTeamAPercent, nextTeamBPercent, nextPaintedPercent);
            return;
        }

        bool teamAControls = nextTeamAPercent >= controlThresholdPercent && nextTeamAPercent > nextTeamBPercent;
        bool teamBControls = nextTeamBPercent >= controlThresholdPercent && nextTeamBPercent > nextTeamAPercent;

        if (teamAControls == teamBControls)
        {
            SetControlState(Team.None, true, nextTeamAPercent, nextTeamBPercent, nextPaintedPercent);
            return;
        }

        SetControlState(teamAControls ? Team.TeamA : Team.TeamB, false, nextTeamAPercent, nextTeamBPercent, nextPaintedPercent);
    }

    public Bounds GetControlBounds()
    {
        Vector3 size = new Vector3(controlSize.x, controlHeight, controlSize.y);
        Vector3 center = transform.position + Vector3.up * (controlHeight * 0.5f);
        return new Bounds(center, size);
    }

    private void ResolveReferences()
    {
        if (towerRenderer == null)
        {
            towerRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void UpdateRouteProgress(float deltaTime)
    {
        if (contested || controllingTeam == Team.None || deltaTime <= 0f)
        {
            return;
        }

        float direction = controllingTeam == Team.TeamA ? 1f : -1f;
        float previousProgress = routeProgress;
        routeProgress = Mathf.Clamp(routeProgress + direction * moveSpeed * deltaTime, -1f, 1f);

        if (!Mathf.Approximately(previousProgress, routeProgress))
        {
            ApplyRoutePosition();
        }
    }

    private void ApplyRoutePosition()
    {
        Vector3 fallbackCenter = centerPoint != null ? centerPoint.position : transform.position;
        Vector3 start = teamBGoal != null ? teamBGoal.position : fallbackCenter + Vector3.back * 4f;
        Vector3 end = teamAGoal != null ? teamAGoal.position : fallbackCenter + Vector3.forward * 4f;
        float normalizedProgress = (routeProgress + 1f) * 0.5f;
        Vector3 routePosition = Vector3.Lerp(start, end, normalizedProgress);
        routePosition.y = transform.position.y;
        transform.position = routePosition;
    }

    private void SetControlState(Team team, bool isContested, float teamA, float teamB, float painted)
    {
        controllingTeam = team;
        contested = isContested;
        teamAPercent = teamA;
        teamBPercent = teamB;
        paintedPercent = painted;
        ApplyTowerColor();
    }

    private void ApplyTowerColor()
    {
        if (towerRenderer == null)
        {
            return;
        }

        Color color = neutralColor;

        if (contested)
        {
            color = contestedColor;
        }
        else if (controllingTeam == Team.TeamA)
        {
            color = teamAColor;
        }
        else if (controllingTeam == Team.TeamB)
        {
            color = teamBColor;
        }

        towerRenderer.material.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = contested ? contestedColor : controllingTeam == Team.TeamA ? teamAColor : controllingTeam == Team.TeamB ? teamBColor : neutralColor;
        Bounds bounds = GetControlBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (teamBGoal != null && teamAGoal != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(teamBGoal.position, teamAGoal.position);
        }
    }
}
