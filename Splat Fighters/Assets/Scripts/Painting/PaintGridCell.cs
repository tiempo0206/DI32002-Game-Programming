using System;
using UnityEngine;

/// <summary>
/// Runtime data for one paint grid cell.
/// This is intentionally data-only: visual rendering is handled elsewhere.
/// </summary>
[Serializable]
public class PaintGridCell
{
    [SerializeField] private Team owner = Team.None;

    public Team Owner => owner;

    public bool IsPainted => owner != Team.None;

    public void SetOwner(Team newOwner)
    {
        owner = newOwner;
    }

    public void Clear()
    {
        owner = Team.None;
    }
}
