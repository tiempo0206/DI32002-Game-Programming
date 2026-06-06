using System;
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

    [Header("Ink Resource")]
    [SerializeField] private bool useInkResource = true;
    [SerializeField, Min(0.1f)] private float maxInk = 100f;
    [SerializeField, Min(0f)] private float inkPerShot = 10f;
    [SerializeField, Min(0f)] private float inkRecoveryPerSecond = 12f;
    [SerializeField, Min(1f)] private float ownPaintRecoveryMultiplier = 3.5f;
    [SerializeField] private bool startWithFullInk = true;
    [SerializeField] private Transform groundProbe = null;

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
    private float currentInk;
    private bool isReceivingOwnPaintRecovery;
    private float externalRecoveryMultiplier = 1f;
    private bool externalFireBlocked;
    private bool hasExternalAimDirection;
    private Vector3 externalAimDirection = Vector3.forward;
    private bool hasExternalAimTarget;
    private Vector3 externalAimTarget;
    private Collider[] cachedIgnoredColliders;
    private MaterialPropertyBlock projectileColorBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public Transform FirePoint => firePoint != null ? firePoint : transform;
    public Team Team => team;
    public float CurrentInk => useInkResource ? currentInk : maxInk;
    public float MaxInk => maxInk;
    public float InkPercent => maxInk <= 0f ? 0f : CurrentInk / maxInk * 100f;
    public bool HasEnoughInkToFire => !useInkResource || currentInk >= inkPerShot;
    public bool IsReceivingOwnPaintRecovery => isReceivingOwnPaintRecovery;
    public float ExternalRecoveryMultiplier => externalRecoveryMultiplier;
    public bool IsExternalFireBlocked => externalFireBlocked;
    public event Action Fired;

    private void Awake()
    {
        ResetInkResource();
    }

    private void Update()
    {
        RecoverInk(Time.deltaTime);

        // MVP test input. This keeps the shooting chain testable before the full player input system exists.
        if (!externalFireBlocked && enableKeyboardTestFire && Input.GetKey(testFireKey))
        {
            TryFire();
        }
    }

    /// <summary>
    /// External input, player controllers, or bots can call this method to attempt firing.
    /// </summary>
    public bool TryFire()
    {
        if (externalFireBlocked)
        {
            return false;
        }

        if (Time.time < nextFireTime)
        {
            return false;
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("InkWeapon has no projectile prefab assigned.", this);
            return false;
        }

        if (!TryConsumeInkForShot())
        {
            return false;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 fireDirection = GetFireDirection(spawnPoint);
        Quaternion rotation = Quaternion.LookRotation(fireDirection, Vector3.up);
        bool paintedDirectly = PaintDirectlyAtAimTargetIfNeeded();
        bool projectileCanPaint = !paintedDirectly || !projectileIsVisualOnlyWhenDirectPainting;

        InkProjectile projectile = Instantiate(projectilePrefab, spawnPoint.position, rotation);
        ApplyProjectileTeamColor(projectile);
        projectile.IgnoreColliders(GetIgnoredColliders());
        projectile.Launch(
            fireDirection,
            projectileSpeed,
            paintRadius,
            team,
            externalAimTarget,
            hasExternalAimTarget,
            projectileCanPaint);

        nextFireTime = Time.time + fireCooldown;
        Fired?.Invoke();
        SplatAudioManager.PlayWeaponFireSound();
        return true;
    }

    public void ResetInkResource()
    {
        currentInk = startWithFullInk ? maxInk : Mathf.Min(currentInk, maxInk);
        isReceivingOwnPaintRecovery = false;
        externalRecoveryMultiplier = 1f;
        externalFireBlocked = false;
    }

    public void SetExternalRecoveryMultiplier(float multiplier)
    {
        externalRecoveryMultiplier = Mathf.Max(1f, multiplier);
    }

    public void SetExternalFireBlocked(bool blocked)
    {
        externalFireBlocked = blocked;
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

    public void RefreshIgnoredColliderCache()
    {
        cachedIgnoredColliders = GetComponentsInChildren<Collider>();
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

    private bool TryConsumeInkForShot()
    {
        if (!useInkResource)
        {
            return true;
        }

        if (currentInk < inkPerShot)
        {
            return false;
        }

        currentInk = Mathf.Max(0f, currentInk - inkPerShot);
        return true;
    }

    private void RecoverInk(float deltaTime)
    {
        if (!useInkResource)
        {
            isReceivingOwnPaintRecovery = false;
            currentInk = maxInk;
            return;
        }

        currentInk = Mathf.Clamp(currentInk, 0f, maxInk);

        bool onOwnPaint = IsStandingOnOwnPaint();
        isReceivingOwnPaintRecovery = currentInk < maxInk && onOwnPaint;

        if (currentInk >= maxInk || deltaTime <= 0f)
        {
            return;
        }

        float recoveryRate = inkRecoveryPerSecond;

        if (onOwnPaint)
        {
            recoveryRate *= ownPaintRecoveryMultiplier;
        }

        recoveryRate *= externalRecoveryMultiplier;
        currentInk = Mathf.Min(maxInk, currentInk + recoveryRate * deltaTime);
    }

    private bool IsStandingOnOwnPaint()
    {
        if (PaintManager.Instance == null || team == Team.None)
        {
            return false;
        }

        if (!PaintManager.Instance.TryGetTeamAtWorldPosition(GetGroundProbePosition(), out Team groundTeam))
        {
            return false;
        }

        return groundTeam == team;
    }

    private Vector3 GetGroundProbePosition()
    {
        if (groundProbe != null)
        {
            return groundProbe.position;
        }

        return transform.position;
    }

    private void ApplyProjectileTeamColor(InkProjectile projectile)
    {
        if (!applyTeamColorToProjectile || projectile == null)
        {
            return;
        }

        Renderer[] renderers = projectile.GetComponentsInChildren<Renderer>();
        if (projectileColorBlock == null)
        {
            projectileColorBlock = new MaterialPropertyBlock();
        }

        Color fallbackColor = team == Team.TeamB ? teamBProjectileColor : teamAProjectileColor;
        Color color = team == Team.None ? fallbackColor : TeamVisualPalette.GetColor(team);
        projectileColorBlock.Clear();
        projectileColorBlock.SetColor(BaseColorId, color);
        projectileColorBlock.SetColor(ColorId, color);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer projectileRenderer = renderers[i];

            if (projectileRenderer == null)
            {
                continue;
            }

            projectileRenderer.SetPropertyBlock(projectileColorBlock);
        }
    }

    private Collider[] GetIgnoredColliders()
    {
        if (cachedIgnoredColliders == null || cachedIgnoredColliders.Length == 0)
        {
            RefreshIgnoredColliderCache();
        }

        return cachedIgnoredColliders;
    }
}
