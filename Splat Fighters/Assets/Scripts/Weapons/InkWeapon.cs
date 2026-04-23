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

    [Header("Quick Test Input")]
    [SerializeField] private bool enableKeyboardTestFire = true;
    [SerializeField] private KeyCode testFireKey = KeyCode.Mouse0;

    private float nextFireTime;

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

        InkProjectile projectile = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        projectile.IgnoreColliders(GetComponentsInChildren<Collider>());
        projectile.Launch(fireDirection, projectileSpeed, paintRadius, team);

        nextFireTime = Time.time + fireCooldown;
        return true;
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
}
