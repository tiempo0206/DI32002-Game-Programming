using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central entry point for painting and coverage scoring.
/// Projectiles should call PaintManager.Instance.PaintAtWorldPosition(...).
/// </summary>
[DisallowMultipleComponent]
public class PaintManager : MonoBehaviour
{
    public static PaintManager Instance { get; private set; }

    [Header("Area References")]
    [SerializeField] private bool autoFindAreasOnAwake = true;
    [SerializeField] private List<PaintableArea> paintableAreas = new List<PaintableArea>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("More than one PaintManager exists. The latest one will become the active instance.", this);
        }

        Instance = this;

        if (autoFindAreasOnAwake)
        {
            RefreshPaintableAreas();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Paints every registered area that contains the world position.
    /// Returns the total number of cells that changed owner.
    /// </summary>
    public int PaintAtWorldPosition(Vector3 worldPosition, float radius, Team team)
    {
        int totalChangedCells = 0;

        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            if (!area.CanPaintAtWorldPosition(worldPosition))
            {
                continue;
            }

            totalChangedCells += area.PaintAtWorldPosition(worldPosition, radius, team);
        }

        return totalChangedCells;
    }

    /// <summary>
    /// Returns total coverage across all registered paintable areas.
    /// The result is a percentage from 0 to 100.
    /// </summary>
    public float GetCoveragePercent(Team team)
    {
        int totalCells = GetTotalCellCount();

        if (totalCells <= 0)
        {
            return 0f;
        }

        return GetCellCount(team) * 100f / totalCells;
    }

    public int GetCellCount(Team team)
    {
        int count = 0;

        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            count += area.GetCellCount(team);
        }

        return count;
    }

    public int GetTotalCellCount()
    {
        int total = 0;

        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            total += area.TotalCellCount;
        }

        return total;
    }

    public Team GetLeadingTeam()
    {
        int teamA = GetCellCount(Team.TeamA);
        int teamB = GetCellCount(Team.TeamB);

        if (teamA == teamB)
        {
            return Team.None;
        }

        return teamA > teamB ? Team.TeamA : Team.TeamB;
    }

    public bool CanPaintAtWorldPosition(Vector3 worldPosition)
    {
        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            if (area.CanPaintAtWorldPosition(worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetTeamAtWorldPosition(Vector3 worldPosition, out Team team)
    {
        team = Team.None;

        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            if (area.TryGetOwnerAtWorldPosition(worldPosition, out team))
            {
                return true;
            }
        }

        return false;
    }

    public void RegisterArea(PaintableArea area)
    {
        if (area == null || paintableAreas.Contains(area))
        {
            return;
        }

        paintableAreas.Add(area);
    }

    public void UnregisterArea(PaintableArea area)
    {
        if (area == null)
        {
            return;
        }

        paintableAreas.Remove(area);
    }

    [ContextMenu("Refresh Paintable Areas")]
    public void RefreshPaintableAreas()
    {
        paintableAreas.Clear();
        PaintableArea[] foundAreas = FindObjectsOfType<PaintableArea>();

        for (int i = 0; i < foundAreas.Length; i++)
        {
            RegisterArea(foundAreas[i]);
        }
    }

    [ContextMenu("Clear All Paint")]
    public void ClearAllPaint()
    {
        for (int i = 0; i < paintableAreas.Count; i++)
        {
            PaintableArea area = paintableAreas[i];

            if (area == null)
            {
                continue;
            }

            area.ClearPaint();
        }
    }
}
