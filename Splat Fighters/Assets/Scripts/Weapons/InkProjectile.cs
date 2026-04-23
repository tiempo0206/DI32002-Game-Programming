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
    [SerializeField] private float paintRadius = 1.5f;
    [SerializeField] private Team team = Team.TeamA;

    [Header("Collision")]
    [SerializeField] private LayerMask paintableLayers = ~0;
    [SerializeField] private bool destroyOnNonPaintableHit = true;

    private Rigidbody rb;
    private Collider projectileCollider;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        Vector3 hitPoint = transform.position;

        if (collision.contactCount > 0)
        {
            hitPoint = collision.GetContact(0).point;
        }

        HandleHit(hitPoint, collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
        {
            return;
        }

        // Trigger colliders do not provide contact points, so use the projectile position for MVP testing.
        HandleHit(transform.position, other.gameObject);
    }

    /// <summary>
    /// Called by InkWeapon to set movement, paint radius, and team ownership.
    /// </summary>
    public void Launch(Vector3 direction, float speed, float radius, Team ownerTeam)
    {
        hasLaunched = true;
        hasHit = false;

        paintRadius = radius;
        team = ownerTeam;

        Vector3 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        float safeSpeed = Mathf.Max(0f, speed);

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.velocity = safeDirection * safeSpeed;
        transform.forward = safeDirection;

        Destroy(gameObject, lifetime);
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

            Physics.IgnoreCollision(projectileCollider, other, true);
        }
    }

    private void HandleHit(Vector3 hitPoint, GameObject hitObject)
    {
        hasHit = true;

        bool canPaintLayer = IsLayerPaintable(hitObject.layer);

        if (canPaintLayer && PaintManager.Instance != null)
        {
            PaintManager.Instance.PaintAtWorldPosition(hitPoint, paintRadius, team);
        }
        else if (!canPaintLayer && !destroyOnNonPaintableHit)
        {
            hasHit = false;
            return;
        }

        Destroy(gameObject);
    }

    private bool IsLayerPaintable(int layer)
    {
        return (paintableLayers.value & (1 << layer)) != 0;
    }
}
