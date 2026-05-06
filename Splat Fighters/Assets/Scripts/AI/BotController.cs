using UnityEngine;

/// <summary>
/// Simple single-player bot for the MVP.
/// It patrols fixed waypoints and fires at fixed ground targets with the existing InkWeapon.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class BotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController = null;
    [SerializeField] private InkWeapon weapon = null;
    [SerializeField] private Transform firePoint = null;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private bool patrolOnStart = true;
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float waypointReachDistance = 0.6f;

    [Header("Shooting")]
    [SerializeField] private bool fireOnStart = true;
    [SerializeField] private Transform[] paintTargets = new Transform[0];
    [SerializeField] private float fireInterval = 0.65f;
    [SerializeField] private float aimRefreshInterval = 1.2f;
    [SerializeField] private float fallbackAimDistance = 4f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color waypointGizmoColor = new Color(1f, 0.45f, 0.05f, 0.85f);
    [SerializeField] private Color aimGizmoColor = new Color(1f, 0.75f, 0.05f, 0.85f);

    private int currentWaypointIndex;
    private int currentPaintTargetIndex;
    private float verticalVelocity;
    private float nextFireTime;
    private float nextAimRefreshTime;
    private Vector3 currentAimTarget;

    private void Awake()
    {
        ResolveReferences();
        currentAimTarget = ResolveCurrentAimTarget();
    }

    private void Update()
    {
        ResolveReferences();
        UpdatePatrol();
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

        if (firePoint == null && weapon != null)
        {
            firePoint = weapon.FirePoint;
        }
    }

    private void UpdatePatrol()
    {
        ApplyGravity();

        Vector3 horizontalMove = Vector3.zero;

        if (patrolOnStart && waypoints.Length > 0)
        {
            Transform waypoint = waypoints[currentWaypointIndex];

            if (waypoint != null)
            {
                Vector3 toWaypoint = waypoint.position - transform.position;
                toWaypoint.y = 0f;

                if (toWaypoint.magnitude <= waypointReachDistance)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                }
                else
                {
                    horizontalMove = toWaypoint.normalized * moveSpeed;
                    RotateToward(toWaypoint.normalized);
                }
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
        if (!fireOnStart || weapon == null || Time.time < nextFireTime)
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
