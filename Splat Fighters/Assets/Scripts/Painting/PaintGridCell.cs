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
    [SerializeField] private bool isPaintable = true;

    public Team Owner => owner;
    public bool IsPaintable => isPaintable;

    public bool IsPainted => isPaintable && owner != Team.None;

    public void SetOwner(Team newOwner)
    {
        if (!isPaintable)
        {
            owner = Team.None;
            return;
        }

        owner = newOwner;
    }

    public void SetPaintable(bool value)
    {
        isPaintable = value;

        if (!isPaintable)
        {
            owner = Team.None;
        }
    }

    public void Clear()
    {
        owner = Team.None;
    }
}
