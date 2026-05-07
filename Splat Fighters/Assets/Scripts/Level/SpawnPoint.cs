using UnityEngine;

/// <summary>
/// Simple team spawn marker for players and future bots.
/// The transform position is the character root position, not the floor pad position.
/// </summary>
[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private Team team = Team.TeamA;
    [SerializeField] private bool defaultForTeam = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField, Min(0.05f)] private float gizmoRadius = 0.45f;
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAGizmoColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBGizmoColor;
    [SerializeField] private Color neutralColor = TeamVisualPalette.NeutralColor;

    public Team Team => team;
    public bool DefaultForTeam => defaultForTeam;
    public Vector3 SpawnPosition => transform.position;
    public Quaternion SpawnRotation => transform.rotation;

    public void Configure(Team newTeam, bool newDefaultForTeam)
    {
        team = newTeam;
        defaultForTeam = newDefaultForTeam;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = GetGizmoColor();
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawRay(transform.position, transform.forward * (gizmoRadius * 2.5f));
    }

    private Color GetGizmoColor()
    {
        switch (team)
        {
            case Team.TeamA:
                return teamAColor;
            case Team.TeamB:
                return teamBColor;
            default:
                return neutralColor;
        }
    }
}
