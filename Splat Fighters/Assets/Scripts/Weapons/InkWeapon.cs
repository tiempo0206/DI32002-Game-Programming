using UnityEngine;

/// <summary>
/// Minimal weapon script: spawns ink projectiles and passes team and paint settings to them.
/// </summary>
public class InkWeapon : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private InkProjectile projectilePrefab = null;
    [SerializeField] private Transform firePoint = null;
    [SerializeField] private Team team = Team.TeamA;

    [Header("Weapon Stats")]
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float paintRadius = 1.5f;
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Aiming")]
    [SerializeField] private bool useCameraAim = false;
    [SerializeField] private Camera aimCamera = null;
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimLayers = ~0;

    [Header("Crosshair Paint")]
    [SerializeField] private bool paintDirectlyAtAimTarget = true;
    [SerializeField] private bool projectileIsVisualOnlyWhenDirectPainting = true;

    [Header("Projectile Visuals")]
    [SerializeField] private bool applyTeamColorToProjectile = true;
    [SerializeField] private Color teamAProjectileColor = TeamVisualPalette.TeamAColor;
    [SerializeField] private Color teamBProjectileColor = TeamVisualPalette.TeamBColor;

    [Header("Quick Test Input")]
    [SerializeField] private bool enableKeyboardTestFire = true;
    [SerializeField] private KeyCode testFireKey = KeyCode.Mouse0;

    private float nextFireTime;
    private bool hasExternalAimDirection;
    private Vector3 externalAimDirection = Vector3.forward;
    private bool hasExternalAimTarget;
    private Vector3 externalAimTarget;

    public Transform FirePoint => firePoint != null ? firePoint : transform;

    private void Update()
    {
        // MVP test input. This keeps the shooting chain testable before the full player input system exists.
        if (enableKeyboardTestFire && Input.GetKey(testFireKey))
        {
            TryFire();
        }
    }

    /// <summary>
    /// External input, player controllers, or bots can call this method to attempt firing.
    /// </summary>
    public bool TryFire()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("InkWeapon has no projectile prefab assigned.", this);
            return false;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 fireDirection = GetFireDirection(spawnPoint);
        Quaternion rotation = Quaternion.LookRotation(fireDirection, Vector3.up);
        bool paintedDirectly = PaintDirectlyAtAimTargetIfNeeded();
        bool projectileCanPaint = !paintedDirectly || !projectileIsVisualOnlyWhenDirectPainting;

        InkProjectile projectile = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        ApplyProjectileTeamColor(projectile);
        projectile.IgnoreColliders(GetComponentsInChildren<Collider>());
        projectile.Launch(
            fireDirection,
            projectileSpeed,
            paintRadius,
            team,
            externalAimTarget,
            hasExternalAimTarget,
            projectileCanPaint);

        nextFireTime = Time.time + fireCooldown;
        return true;
    }

    /// <summary>
    /// Called by AimController so the weapon can stay simple and only spawn projectiles.
    /// </summary>
    public void SetAimDirection(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        externalAimDirection = worldDirection.normalized;
        hasExternalAimDirection = true;
    }

    /// <summary>
    /// Called by AimController so projectiles can resolve near-ground crosshair hits reliably.
    /// </summary>
    public void SetAimTarget(Vector3 worldPoint, bool hasTarget)
    {
        externalAimTarget = worldPoint;
        hasExternalAimTarget = hasTarget;
    }

    public void ClearAimDirection()
    {
        hasExternalAimDirection = false;
        hasExternalAimTarget = false;
    }

    /// <summary>
    /// Allows one-shot testing from the component context menu.
    /// </summary>
    [ContextMenu("Debug Fire Once")]
    private void DebugFireOnce()
    {
        TryFire();
    }

    private Vector3 GetFireDirection(Transform spawnPoint)
    {
        if (hasExternalAimDirection)
        {
            if (hasExternalAimTarget)
            {
                Vector3 directionToTarget = externalAimTarget - spawnPoint.position;

                if (directionToTarget.sqrMagnitude > 0.0001f)
                {
                    return directionToTarget.normalized;
                }
            }

            return externalAimDirection;
        }

        if (!useCameraAim || aimCamera == null)
        {
            return spawnPoint.forward;
        }

        Ray ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 directionToHit = hit.point - spawnPoint.position;

            if (directionToHit.sqrMagnitude > 0.0001f)
            {
                return directionToHit.normalized;
            }
        }

        Vector3 fallbackTarget = ray.origin + ray.direction * maxAimDistance;
        return (fallbackTarget - spawnPoint.position).normalized;
    }

    private bool PaintDirectlyAtAimTargetIfNeeded()
    {
        if (!paintDirectlyAtAimTarget || !hasExternalAimTarget || PaintManager.Instance == null)
        {
            return false;
        }

        if (!PaintManager.Instance.CanPaintAtWorldPosition(externalAimTarget))
        {
            return false;
        }

        PaintManager.Instance.PaintAtWorldPosition(externalAimTarget, paintRadius, team);
        return true;
    }

    private void ApplyProjectileTeamColor(InkProjectile projectile)
    {
        if (!applyTeamColorToProjectile || projectile == null)
        {
            return;
        }

        Renderer[] renderers = projectile.GetComponentsInChildren<Renderer>();
        Color color = team == Team.TeamB ? teamBProjectileColor : teamAProjectileColor;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer projectileRenderer = renderers[i];

            if (projectileRenderer == null)
            {
                continue;
            }

            projectileRenderer.material.color = color;
        }
    }
}
