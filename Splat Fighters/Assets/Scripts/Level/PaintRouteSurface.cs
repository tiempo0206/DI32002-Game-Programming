using UnityEngine;

/// <summary>
/// Enables a limited traversal route when its paint probe belongs to the required team.
/// This keeps vertical traversal tied to territory ownership without introducing full wall-painting.
/// </summary>
[DisallowMultipleComponent]
public class PaintRouteSurface : MonoBehaviour
{
    [Header("Route Ownership")]
    [SerializeField] private Team requiredTeam = Team.TeamA;
    [SerializeField] private Transform ownershipProbe = null;

    [Header("Route Motion")]
    [SerializeField] private Vector3 routeDirection = Vector3.up;
    [SerializeField, Min(0.1f)] private float routeSpeed = 4f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color inactiveGizmoColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color activeGizmoColor = TeamVisualPalette.TeamAGizmoColor;

    public Team RequiredTeam => requiredTeam;
    public float RouteSpeed => routeSpeed;
    public Vector3 RouteDirection => routeDirection.sqrMagnitude <= 0.0001f ? Vector3.up : routeDirection.normalized;
    public Vector3 ProbePosition => ownershipProbe != null ? ownershipProbe.position : transform.position;

    public void Configure(Team team, Transform probe, Vector3 direction, float speed)
    {
        requiredTeam = team;
        ownershipProbe = probe;
        routeDirection = direction.sqrMagnitude <= 0.0001f ? Vector3.up : direction.normalized;
        routeSpeed = Mathf.Max(0.1f, speed);
    }

    public bool IsActiveForTeam(Team team)
    {
        if (team == Team.None || team != requiredTeam || PaintManager.Instance == null)
        {
            return false;
        }

        return PaintManager.Instance.TryGetTeamAtWorldPosition(ProbePosition, out Team owner) && owner == team;
    }

    public Vector3 GetRouteVelocity()
    {
        return RouteDirection * routeSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = IsActiveForTeam(requiredTeam) ? activeGizmoColor : inactiveGizmoColor;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        Gizmos.DrawSphere(ProbePosition, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + RouteDirection * 1.4f);
    }
}
