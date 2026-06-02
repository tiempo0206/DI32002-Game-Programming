using UnityEngine;

/// <summary>
/// Lightweight Splat Zones-style objective that reads local paint ownership inside a center bounds.
/// </summary>
[DisallowMultipleComponent]
public class SplatZoneObjective : MonoBehaviour
{
    [Header("Zone Bounds")]
    [SerializeField] private Vector2 zoneSize = new Vector2(5f, 3f);
    [SerializeField, Min(0.01f)] private float zoneHeight = 0.5f;

    [Header("Control Rules")]
    [SerializeField, Range(0f, 100f)] private float controlThresholdPercent = 55f;
    [SerializeField, Range(0f, 100f)] private float minimumPaintedPercent = 20f;
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.25f;

    [Header("Visuals")]
    [SerializeField] private Renderer zoneRenderer = null;
    [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 0.28f);
    [SerializeField] private Color contestedColor = new Color(1f, 0.95f, 0.2f, 0.38f);
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAOverlayColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBOverlayColor;

    private float nextRefreshTime;
    private Team controllingTeam = Team.None;
    private bool contested;
    private float teamAPercent;
    private float teamBPercent;
    private float paintedPercent;

    public Team ControllingTeam => controllingTeam;
    public bool IsContested => contested;
    public float TeamAPercent => teamAPercent;
    public float TeamBPercent => teamBPercent;
    public float PaintedPercent => paintedPercent;

    private void Awake()
    {
        ResolveReferences();
        RefreshZoneState();
    }

    private void Update()
    {
        if (Time.time < nextRefreshTime)
        {
            return;
        }

        RefreshZoneState();
    }

    public void RefreshZoneState()
    {
        nextRefreshTime = Time.time + refreshInterval;

        if (PaintManager.Instance == null || !PaintManager.Instance.GetTeamCellCountsInWorldBounds(GetZoneBounds(), out int totalCells, out int teamACells, out int teamBCells))
        {
            SetZoneState(Team.None, false, 0f, 0f, 0f);
            return;
        }

        teamAPercent = teamACells * 100f / totalCells;
        teamBPercent = teamBCells * 100f / totalCells;
        paintedPercent = (teamACells + teamBCells) * 100f / totalCells;

        if (paintedPercent < minimumPaintedPercent)
        {
            SetZoneState(Team.None, false, teamAPercent, teamBPercent, paintedPercent);
            return;
        }

        bool teamAControls = teamAPercent >= controlThresholdPercent && teamAPercent > teamBPercent;
        bool teamBControls = teamBPercent >= controlThresholdPercent && teamBPercent > teamAPercent;

        if (teamAControls == teamBControls)
        {
            SetZoneState(Team.None, true, teamAPercent, teamBPercent, paintedPercent);
            return;
        }

        SetZoneState(teamAControls ? Team.TeamA : Team.TeamB, false, teamAPercent, teamBPercent, paintedPercent);
    }

    public Bounds GetZoneBounds()
    {
        Vector3 size = new Vector3(zoneSize.x, zoneHeight, zoneSize.y);
        Vector3 center = transform.position + Vector3.up * (zoneHeight * 0.5f);
        return new Bounds(center, size);
    }

    private void ResolveReferences()
    {
        if (zoneRenderer == null)
        {
            zoneRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void SetZoneState(Team team, bool isContested, float teamA, float teamB, float painted)
    {
        controllingTeam = team;
        contested = isContested;
        teamAPercent = teamA;
        teamBPercent = teamB;
        paintedPercent = painted;
        ApplyZoneColor();
    }

    private void ApplyZoneColor()
    {
        if (zoneRenderer == null)
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
            color = TeamVisualPalette.GetColor(Team.TeamA, teamAColor.a);
        }
        else if (controllingTeam == Team.TeamB)
        {
            color = TeamVisualPalette.GetColor(Team.TeamB, teamBColor.a);
        }

        zoneRenderer.material.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = contested ? contestedColor : controllingTeam == Team.TeamA ? teamAColor : controllingTeam == Team.TeamB ? teamBColor : neutralColor;
        Bounds bounds = GetZoneBounds();
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
