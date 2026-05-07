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

    public static readonly Color TeamAGizmoColor = WithAlpha(TeamAColor, 0.85f);
    public static readonly Color TeamBGizmoColor = WithAlpha(TeamBColor, 0.85f);
    public static readonly Color TeamAOverlayColor = WithAlpha(TeamAColor, 0.9f);
    public static readonly Color TeamBOverlayColor = WithAlpha(TeamBColor, 0.9f);

    public static Color GetColor(Team team, float alpha = 1f)
    {
        switch (team)
        {
            case Team.TeamA:
                return WithAlpha(TeamAColor, alpha);
            case Team.TeamB:
                return WithAlpha(TeamBColor, alpha);
            default:
                return WithAlpha(NeutralColor, alpha);
        }
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
}
