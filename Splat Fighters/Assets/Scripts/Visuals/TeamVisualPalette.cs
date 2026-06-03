using UnityEngine;

/// <summary>
/// Shared colors and labels for team-readable gameplay visuals.
/// </summary>
public static class TeamVisualPalette
{
    public const string TeamALabel = "Team A";
    public const string TeamBLabel = "Team B";

    public static readonly Color TeamAColor = new Color(0.05f, 0.45f, 1f, 1f);
    public static readonly Color TeamBColor = new Color(1f, 0.45f, 0.05f, 1f);
    public static readonly Color NeutralColor = Color.white;

    public static Color TeamAGizmoColor => GetColor(Team.TeamA, 0.85f);
    public static Color TeamBGizmoColor => GetColor(Team.TeamB, 0.85f);
    public static Color TeamAOverlayColor => GetColor(Team.TeamA, 0.9f);
    public static Color TeamBOverlayColor => GetColor(Team.TeamB, 0.9f);

    public static Color GetColor(Team team, float alpha = 1f)
    {
        switch (team)
        {
            case Team.TeamA:
                return WithAlpha(LoadSelectedColor(team, TeamAColor), alpha);
            case Team.TeamB:
                return WithAlpha(LoadSelectedColor(team, TeamBColor), alpha);
            default:
                return WithAlpha(NeutralColor, alpha);
        }
    }

    public static void SaveSelectedColor(Team team, Color color)
    {
        string keyPrefix = GetColorKeyPrefix(team);
        if (string.IsNullOrEmpty(keyPrefix))
        {
            return;
        }

        PlayerPrefs.SetInt($"{keyPrefix}.Configured", 1);
        PlayerPrefs.SetFloat($"{keyPrefix}.R", color.r);
        PlayerPrefs.SetFloat($"{keyPrefix}.G", color.g);
        PlayerPrefs.SetFloat($"{keyPrefix}.B", color.b);
    }

    public static string GetLabel(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return TeamALabel;
            case Team.TeamB:
                return TeamBLabel;
            default:
                return "No team";
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color LoadSelectedColor(Team team, Color fallback)
    {
        string keyPrefix = GetColorKeyPrefix(team);
        if (string.IsNullOrEmpty(keyPrefix) || PlayerPrefs.GetInt($"{keyPrefix}.Configured", 0) == 0)
        {
            return fallback;
        }

        return new Color(
            PlayerPrefs.GetFloat($"{keyPrefix}.R", fallback.r),
            PlayerPrefs.GetFloat($"{keyPrefix}.G", fallback.g),
            PlayerPrefs.GetFloat($"{keyPrefix}.B", fallback.b),
            1f);
    }

    private static string GetColorKeyPrefix(Team team)
    {
        switch (team)
        {
            case Team.TeamA:
                return "SplatFighters.TeamAInkColor";
            case Team.TeamB:
                return "SplatFighters.TeamBInkColor";
            default:
                return string.Empty;
        }
    }
}
