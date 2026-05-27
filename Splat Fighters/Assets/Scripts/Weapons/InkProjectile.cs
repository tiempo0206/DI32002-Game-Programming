using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ink projectile: handles physics movement, impact detection, and paint triggering.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InkProjectile : MonoBehaviour
{
    [Header("Default Settings")]
    [SerializeField] private float defaultSpeed = 18f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField, Min(0.05f)] private float visualOnlyLifetime = 0.35f;
    [SerializeField] private float paintRadius = 1.5f;
    [SerializeField] private Team team = Team.TeamA;

    [Header("Collision")]
    [SerializeField] private LayerMask paintableLayers = ~0;
    [SerializeField] private bool destroyOnNonPaintableHit = true;

    [Header("Reliable Hit Detection")]
    [SerializeField] private bool useSweepHitDetection = true;
    [SerializeField, Min(0.01f)] private float sweepRadius = 0.12f;
    [SerializeField, Min(0f)] private float sweepExtraDistance = 0.08f;
    [SerializeField] private bool useIntendedImpactPoint = true;
    [SerializeField] private bool paintAtIntendedImpactPoint = true;
    [SerializeField, Min(0.01f)] private float intendedImpactPointTolerance = 0.35f;

    [Header("Hit Feedback")]
    [SerializeField] private bool spawnImpactMarker = false;
    [SerializeField] private Color impactMarkerColor = new Color(0.05f, 0.65f, 1f, 0.85f);
    [SerializeField] private Color noPaintMarkerColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField, Min(0.01f)] private float impactMarkerSize = 0.35f;
    [SerializeField, Min(0.01f)] private float impactMarkerLifetime = 0.25f;
    [SerializeField, Min(0f)] private float impactMarkerSurfaceOffset = 0.03f;
    [SerializeField] private bool logPaintMisses = false;

    [Header("Ink Splatter VFX")]
    [SerializeField] private bool spawnInkSplatterVfx = false;
    [SerializeField] private bool spawnSplatterOnNonPaintableHit = true;
    [SerializeField, Min(0.1f)] private float splatterRadiusMultiplier = 1.1f;

    private Rigidbody rb;
    private Collider projectileCollider;
    private readonly List<Collider> ignoredColliders = new List<Collider>();
    private readonly RaycastHit[] sweepHits = new RaycastHit[16];
    private Vector3 previousPosition;
    private Vector3 intendedImpactPoint;
    private bool hasIntendedImpactPoint;
    private bool canPaintOnImpact = true;
    private bool hasLaunched;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        // If the projectile is placed directly in the scene, make it move forward for testing.
        if (!hasLaunched)
        {
            Launch(transform.forward, defaultSpeed, paintRadius, team);
        }
    }

    private void FixedUpdate()
    {
        if (!hasLaunched || hasHit || !useSweepHitDetection || rb == null)
        {
            return;
        }

        Vector3 currentPosition = rb.position;
        Vector3 travel = currentPosition - previousPosition;
        float travelDistance = travel.magnitude;

        if (travelDistance <= 0.0001f)
        {
            previousPosition = currentPosition;
            return;
        }

        Vector3 travelDirection = travel / travelDistance;

        if (TryFindSweepHit(previousPosition, travelDirection, travelDistance + sweepExtraDistance, out RaycastHit hit))
        {
            HandleHit(hit.point, hit.collider.gameObject, hit.normal);
            return;
        }

        if (ShouldUseIntendedImpactPoint(previousPosition, currentPosition))
        {
            HandleIntendedImpact();
            return;
        }

        previousPosition = currentPosition;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        Vector3 hitPoint = transform.position;
        Vector3 hitNormal = -transform.forward;

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            hitPoint = contact.point;
            hitNormal = contact.normal;
        }

        HandleHit(hitPoint, collision.gameObject, hitNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
        {
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitNormal = (transform.position - hitPoint).sqrMagnitude > 0.0001f
            ? (transform.position - hitPoint).normalized
            : -transform.forward;

        HandleHit(hitPoint, other.gameObject, hitNormal);
    }

    /// <summary>
    /// Called by InkWeapon to set movement, paint radius, and team ownership.
    /// </summary>
    public void Launch(Vector3 direction, float speed, float radius, Team ownerTeam)
    {
        Launch(direction, speed, radius, ownerTeam, Vector3.zero, false);
    }

    /// <summary>
    /// Called by InkWeapon to set movement, paint radius, team ownership, and optional crosshair target.
    /// </summary>
    public void Launch(Vector3 direction, float speed, float radius, Team ownerTeam, Vector3 targetPoint, bool hasTargetPoint)
    {
        Launch(direction, speed, radius, ownerTeam, targetPoint, hasTargetPoint, true);
    }

    /// <summary>
    /// Called by InkWeapon when direct crosshair painting makes the projectile visual-only.
    /// </summary>
    public void Launch(Vector3 direction, float speed, float radius, Team ownerTeam, Vector3 targetPoint, bool hasTargetPoint, bool canPaint)
    {
        hasLaunched = true;
        hasHit = false;

        paintRadius = radius;
        team = ownerTeam;
        intendedImpactPoint = targetPoint;
        hasIntendedImpactPoint = hasTargetPoint;
        canPaintOnImpact = canPaint;
        ApplyLiquidProjectileVisual();

        Vector3 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        float safeSpeed = Mathf.Max(0f, speed);

        if (projectileCollider == null)
        {
            projectileCollider = GetComponent<Collider>();
        }

        if (projectileCollider != null)
        {
            projectileCollider.enabled = canPaintOnImpact;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.velocity = safeDirection * safeSpeed;
        transform.forward = safeDirection;
        previousPosition = rb.position;

        Destroy(gameObject, canPaintOnImpact ? lifetime : visualOnlyLifetime);
    }

    /// <summary>
    /// Prevents the projectile from colliding with the shooter that spawned it.
    /// </summary>
    public void IgnoreColliders(Collider[] collidersToIgnore)
    {
        if (projectileCollider == null)
        {
            projectileCollider = GetComponent<Collider>();
        }

        if (projectileCollider == null || collidersToIgnore == null)
        {
            return;
        }

        for (int i = 0; i < collidersToIgnore.Length; i++)
        {
            Collider other = collidersToIgnore[i];

            if (other == null || other == projectileCollider)
            {
                continue;
            }

            if (!ignoredColliders.Contains(other))
            {
                ignoredColliders.Add(other);
            }

            Physics.IgnoreCollision(projectileCollider, other, true);
        }
    }

    private bool TryFindSweepHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit bestHit)
    {
        bestHit = default;
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            sweepRadius,
            direction,
            sweepHits,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore);

        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = sweepHits[i];

            if (IsIgnoredCollider(hit.collider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            return false;
        }

        bestHit = sweepHits[bestIndex];
        return true;
    }

    private void HandleHit(Vector3 hitPoint, GameObject hitObject, Vector3 hitNormal)
    {
        hasHit = true;

        bool canPaintLayer = IsLayerPaintable(hitObject.layer);
        Vector3 paintPoint = GetPaintPoint(hitPoint, canPaintLayer);
        int changedCells = 0;

        if (canPaintOnImpact && canPaintLayer && PaintManager.Instance != null)
        {
            changedCells = PaintManager.Instance.PaintAtWorldPosition(paintPoint, paintRadius, team);
        }
        else if (canPaintOnImpact && !canPaintLayer && !destroyOnNonPaintableHit)
        {
            hasHit = false;
            return;
        }

        SpawnImpactFeedback(hitPoint, hitNormal, canPaintLayer);
        LogPaintMissIfNeeded(canPaintLayer, changedCells, hitObject, paintPoint);
        Destroy(gameObject);
    }

    private bool ShouldUseIntendedImpactPoint(Vector3 segmentStart, Vector3 segmentEnd)
    {
        if (!useIntendedImpactPoint || !hasIntendedImpactPoint)
        {
            return false;
        }

        Vector3 segment = segmentEnd - segmentStart;
        float segmentLengthSqr = segment.sqrMagnitude;

        if (segmentLengthSqr <= 0.0001f)
        {
            return false;
        }

        float t = Vector3.Dot(intendedImpactPoint - segmentStart, segment) / segmentLengthSqr;

        if (t < 0f || t > 1f)
        {
            return false;
        }

        Vector3 closestPoint = segmentStart + segment * t;
        float tolerance = Mathf.Max(intendedImpactPointTolerance, sweepRadius);
        return (closestPoint - intendedImpactPoint).sqrMagnitude <= tolerance * tolerance;
    }

    private void HandleIntendedImpact()
    {
        hasHit = true;

        int changedCells = 0;

        if (canPaintOnImpact && PaintManager.Instance != null)
        {
            changedCells = PaintManager.Instance.PaintAtWorldPosition(intendedImpactPoint, paintRadius, team);
        }

        SpawnImpactFeedback(intendedImpactPoint, Vector3.up, true);
        LogPaintMissIfNeeded(true, changedCells, gameObject, intendedImpactPoint);
        Destroy(gameObject);
    }

    private bool IsIgnoredCollider(Collider other)
    {
        return other == null
            || other == projectileCollider
            || ignoredColliders.Contains(other);
    }

    private bool IsLayerPaintable(int layer)
    {
        return (paintableLayers.value & (1 << layer)) != 0;
    }

    private Vector3 GetPaintPoint(Vector3 physicsHitPoint, bool canPaintLayer)
    {
        if (paintAtIntendedImpactPoint && canPaintLayer && hasIntendedImpactPoint)
        {
            return intendedImpactPoint;
        }

        return physicsHitPoint;
    }

    private void SpawnImpactFeedback(Vector3 hitPoint, Vector3 hitNormal, bool canPaintLayer)
    {
        SpawnImpactMarker(hitPoint, hitNormal, canPaintLayer);
        SpawnInkSplatter(hitPoint, hitNormal, canPaintLayer);
    }

    private void SpawnImpactMarker(Vector3 hitPoint, Vector3 hitNormal, bool canPaintLayer)
    {
        if (!spawnImpactMarker)
        {
            return;
        }

        Vector3 safeNormal = hitNormal.sqrMagnitude > 0.0001f ? hitNormal.normalized : Vector3.up;
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "InkImpactFeedback";
        marker.transform.position = hitPoint + safeNormal * impactMarkerSurfaceOffset;
        marker.transform.localScale = Vector3.one * impactMarkerSize;

        Collider markerCollider = marker.GetComponent<Collider>();

        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();

        if (markerRenderer != null)
        {
            markerRenderer.material.color = canPaintLayer ? impactMarkerColor : noPaintMarkerColor;
        }

        Destroy(marker, impactMarkerLifetime);
    }

    private void SpawnInkSplatter(Vector3 hitPoint, Vector3 hitNormal, bool canPaintLayer)
    {
        if (!spawnInkSplatterVfx || (!canPaintLayer && !spawnSplatterOnNonPaintableHit))
        {
            return;
        }

        InkSplatterVfx.Spawn(hitPoint, hitNormal, team, canPaintLayer, paintRadius * splatterRadiusMultiplier);
    }

    private void ApplyLiquidProjectileVisual()
    {
        LiquidInkProjectileVisual liquidVisual = GetComponent<LiquidInkProjectileVisual>();

        if (liquidVisual == null)
        {
            liquidVisual = gameObject.AddComponent<LiquidInkProjectileVisual>();
        }

        liquidVisual.Configure(team, paintRadius);
    }

    private void LogPaintMissIfNeeded(bool canPaintLayer, int changedCells, GameObject hitObject, Vector3 hitPoint)
    {
        if (!logPaintMisses || !canPaintLayer || changedCells > 0)
        {
            return;
        }

        if (PaintManager.Instance == null)
        {
            Debug.LogWarning("Ink projectile hit a paintable layer, but there is no PaintManager in the scene.", this);
            return;
        }

        Debug.Log(
            $"Ink projectile hit {hitObject.name}, but no paint cells changed at {hitPoint}. This can happen when the area is already painted by the same team or the hit is outside the paint grid.",
            this);
    }
}
