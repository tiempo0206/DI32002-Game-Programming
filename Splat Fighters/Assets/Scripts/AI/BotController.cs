using UnityEngine;

/// <summary>
/// Simple single-player bot for the Turf War MVP.
/// It patrols waypoints, contests enemy paint, and retreats when resources are low.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class BotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController = null;
    [SerializeField] private InkWeapon weapon = null;
    [SerializeField] private CharacterHealth health = null;
    [SerializeField] private Transform firePoint = null;

    [Header("Team")]
    [SerializeField] private Team botTeam = Team.TeamB;
    [SerializeField] private Team priorityPaintTargetTeam = Team.TeamA;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private Transform retreatTarget = null;
    [SerializeField] private bool patrolOnStart = true;
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float waypointReachDistance = 0.6f;

    [Header("Shooting")]
    [SerializeField] private bool fireOnStart = true;
    [SerializeField] private Transform[] paintTargets = new Transform[0];
    [SerializeField] private bool useTerritoryAwareAim = true;
    [SerializeField] private bool targetUnpaintedCellsAfterEnemyPaint = true;
    [SerializeField] private float territorySearchRadius = 16f;
    [SerializeField] private float fireInterval = 0.65f;
    [SerializeField] private float aimRefreshInterval = 1.2f;
    [SerializeField] private float fallbackAimDistance = 4f;

    [Header("Retreat")]
    [SerializeField] private bool retreatWhenPressured = true;
    [SerializeField, Range(0f, 100f)] private float lowInkRetreatPercent = 28f;
    [SerializeField, Range(0f, 100f)] private float resumeInkPercent = 62f;
    [SerializeField, Range(0f, 100f)] private float lowHealthRetreatPercent = 45f;
    [SerializeField] private float retreatReachDistance = 0.9f;
    [SerializeField] private float retreatRecoveryMultiplier = 1.35f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color waypointGizmoColor = TeamVisualPalette.TeamBGizmoColor;
    [SerializeField] private Color aimGizmoColor = new Color(1f, 0.75f, 0.05f, 0.85f);

    private int currentWaypointIndex;
    private int currentPaintTargetIndex;
    private float verticalVelocity;
    private float nextFireTime;
    private float nextAimRefreshTime;
    private Vector3 currentAimTarget;
    private bool isRetreating;

    public Team BotTeam => botTeam;

    private void Awake()
    {
        ResolveReferences();
        currentAimTarget = ResolveCurrentAimTarget();
    }

    private void Update()
    {
        ResolveReferences();
        UpdateTacticalState();
        UpdateMovement();
        UpdateAim();
        UpdateFire();
    }

    public void SetWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints ?? new Transform[0];
        currentWaypointIndex = 0;
    }

    public void SetPaintTargets(Transform[] newPaintTargets)
    {
        paintTargets = newPaintTargets ?? new Transform[0];
        currentPaintTargetIndex = 0;
        currentAimTarget = ResolveCurrentAimTarget();
    }

    public void ResetBotState()
    {
        currentWaypointIndex = 0;
        currentPaintTargetIndex = 0;
        verticalVelocity = 0f;
        nextFireTime = 0f;
        nextAimRefreshTime = 0f;
        isRetreating = false;
        SetWeaponRetreatModifiers(false);
        currentAimTarget = ResolveCurrentAimTarget();
    }

    private void ResolveReferences()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (weapon == null)
        {
            weapon = GetComponentInChildren<InkWeapon>();
        }

        if (health == null)
        {
            health = GetComponent<CharacterHealth>();
        }

        if (firePoint == null && weapon != null)
        {
            firePoint = weapon.FirePoint;
        }
    }

    private void UpdateTacticalState()
    {
        if (!retreatWhenPressured)
        {
            isRetreating = false;
            SetWeaponRetreatModifiers(false);
            return;
        }

        bool lowInk = weapon != null && weapon.InkPercent <= lowInkRetreatPercent;
        bool recoveredInk = weapon == null || weapon.InkPercent >= resumeInkPercent;
        bool onEnemyPaint = IsStandingOnPaintOwnedBy(GetEnemyTeam());
        bool nearRetreatTarget = IsNearRetreatTarget();
        bool lowHealth = health != null && health.HealthPercent <= lowHealthRetreatPercent && !nearRetreatTarget;
        bool recoveredHealth = health == null || health.HealthPercent > lowHealthRetreatPercent || nearRetreatTarget;

        if (isRetreating)
        {
            isRetreating = !(recoveredInk && recoveredHealth && !onEnemyPaint && nearRetreatTarget);
        }
        else
        {
            isRetreating = lowInk || lowHealth || onEnemyPaint;
        }

        SetWeaponRetreatModifiers(isRetreating && !recoveredInk);
    }

    private void SetWeaponRetreatModifiers(bool holdFireForRecovery)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.SetExternalRecoveryMultiplier(isRetreating ? retreatRecoveryMultiplier : 1f);
        weapon.SetExternalFireBlocked(holdFireForRecovery);
    }

    private void UpdateMovement()
    {
        ApplyGravity();

        Vector3 horizontalMove = Vector3.zero;
        Vector3 targetPosition = isRetreating ? ResolveRetreatTarget() : ResolvePatrolTarget();
        float reachDistance = isRetreating ? retreatReachDistance : waypointReachDistance;

        if (patrolOnStart && targetPosition != Vector3.zero)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= reachDistance)
            {
                AdvanceWaypointIfNeeded();
            }
            else
            {
                horizontalMove = toTarget.normalized * moveSpeed;
                RotateToward(toTarget.normalized);
            }
        }

        Vector3 velocity = horizontalMove;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void UpdateAim()
    {
        if (Time.time >= nextAimRefreshTime)
        {
            AdvancePaintTarget();
            currentAimTarget = ResolveCurrentAimTarget();
            nextAimRefreshTime = Time.time + aimRefreshInterval;
        }

        Vector3 aimDirection = currentAimTarget - GetFireOrigin();

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            aimDirection = transform.forward;
        }

        if (weapon != null)
        {
            weapon.SetAimTarget(currentAimTarget, true);
            weapon.SetAimDirection(aimDirection.normalized);
        }
    }

    private void UpdateFire()
    {
        if (!fireOnStart || weapon == null || Time.time < nextFireTime || isRetreating && weapon.InkPercent < resumeInkPercent)
        {
            return;
        }

        weapon.TryFire();
        nextFireTime = Time.time + fireInterval;
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void AdvancePaintTarget()
    {
        if (paintTargets.Length == 0)
        {
            return;
        }

        currentPaintTargetIndex = (currentPaintTargetIndex + 1) % paintTargets.Length;
    }

    private Vector3 ResolveCurrentAimTarget()
    {
        if (useTerritoryAwareAim && PaintManager.Instance != null)
        {
            Vector3 origin = transform.position;

            if (PaintManager.Instance.TryFindNearestCellOwnedBy(priorityPaintTargetTeam, origin, territorySearchRadius, out Vector3 enemyPaintTarget))
            {
                return enemyPaintTarget;
            }

            if (targetUnpaintedCellsAfterEnemyPaint && PaintManager.Instance.TryFindNearestCellOwnedBy(Team.None, origin, territorySearchRadius, out Vector3 unpaintedTarget))
            {
                return unpaintedTarget;
            }
        }

        if (paintTargets.Length > 0)
        {
            Transform target = paintTargets[Mathf.Clamp(currentPaintTargetIndex, 0, paintTargets.Length - 1)];

            if (target != null)
            {
                return target.position;
            }
        }

        Vector3 fallback = transform.position + transform.forward * fallbackAimDistance;
        fallback.y = 0f;
        return fallback;
    }

    private Vector3 ResolvePatrolTarget()
    {
        if (waypoints.Length == 0)
        {
            return Vector3.zero;
        }

        Transform waypoint = waypoints[Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1)];
        return waypoint != null ? waypoint.position : Vector3.zero;
    }

    private Vector3 ResolveRetreatTarget()
    {
        if (retreatTarget != null)
        {
            return retreatTarget.position;
        }

        if (PaintManager.Instance != null && PaintManager.Instance.TryFindNearestCellOwnedBy(botTeam, transform.position, territorySearchRadius, out Vector3 ownPaintTarget))
        {
            ownPaintTarget.y = transform.position.y;
            return ownPaintTarget;
        }

        return ResolvePatrolTarget();
    }

    private void AdvanceWaypointIfNeeded()
    {
        if (isRetreating || waypoints.Length == 0)
        {
            return;
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private bool IsNearRetreatTarget()
    {
        Vector3 toRetreatTarget = ResolveRetreatTarget() - transform.position;
        toRetreatTarget.y = 0f;
        return toRetreatTarget.magnitude <= retreatReachDistance;
    }

    private bool IsStandingOnPaintOwnedBy(Team team)
    {
        if (team == Team.None || PaintManager.Instance == null)
        {
            return false;
        }

        if (!PaintManager.Instance.TryGetTeamAtWorldPosition(transform.position, out Team groundTeam))
        {
            return false;
        }

        return groundTeam == team;
    }

    private Team GetEnemyTeam()
    {
        if (botTeam == Team.TeamA)
        {
            return Team.TeamB;
        }

        if (botTeam == Team.TeamB)
        {
            return Team.TeamA;
        }

        return Team.None;
    }

    private Vector3 GetFireOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        return transform.position + Vector3.up;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = waypointGizmoColor;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];

            if (waypoint == null)
            {
                continue;
            }

            Gizmos.DrawSphere(waypoint.position, 0.18f);

            Transform nextWaypoint = waypoints[(i + 1) % waypoints.Length];

            if (nextWaypoint != null)
            {
                Gizmos.DrawLine(waypoint.position, nextWaypoint.position);
            }
        }

        Gizmos.color = aimGizmoColor;

        for (int i = 0; i < paintTargets.Length; i++)
        {
            Transform target = paintTargets[i];

            if (target != null)
            {
                Gizmos.DrawWireSphere(target.position, 0.28f);
            }
        }
    }
}
