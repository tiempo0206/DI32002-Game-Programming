using UnityEngine;

/// <summary>
/// Converts the camera aim ray into a stable world-space firing direction.
/// This keeps camera logic, aiming logic, and weapon spawning separate for the MVP.
/// </summary>
[DefaultExecutionOrder(80)]
public class AimController : MonoBehaviour
{
    private enum AimInputMode
    {
        ScreenCenter,
        MousePosition
    }

    [Header("References")]
    [SerializeField] private Camera aimCamera = null;
    [SerializeField] private InkWeapon weapon = null;
    [SerializeField] private Transform firePoint = null;
    [SerializeField] private Transform characterRoot = null;
    [SerializeField] private Transform weaponPivot = null;
    [SerializeField] private Transform ignoredRoot = null;
    [SerializeField] private AimReticleUI reticleUI = null;
    [SerializeField] private bool autoCreateReticle = true;

    [Header("Aim Ray")]
    [SerializeField] private AimInputMode aimInputMode = AimInputMode.ScreenCenter;
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float minimumAimDistance = 0.05f;
    [SerializeField] private bool ignoreProjectiles = true;

    [Header("Rotation")]
    [SerializeField] private bool rotateCharacterToAim = true;
    [SerializeField] private bool rotateWeaponPivotToAim = true;
    [SerializeField] private float characterTurnSpeed = 720f;
    [SerializeField] private float weaponTurnSpeed = 1080f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugAimRay = true;
    [SerializeField] private Color debugRayColor = Color.cyan;

    private readonly RaycastHit[] aimHits = new RaycastHit[16];
    private Vector3 currentAimPoint;
    private Vector3 currentAimDirection = Vector3.forward;
    private bool hasAimTarget;

    public Vector3 CurrentAimPoint => currentAimPoint;
    public Vector3 CurrentAimDirection => currentAimDirection;
    public bool HasAimTarget => hasAimTarget;

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        RefreshAimNow();
    }

    public void RefreshAimNow()
    {
        ResolveReferences();

        Vector3 origin = GetAimOrigin();
        currentAimPoint = ResolveAimPoint(origin, out hasAimTarget);
        currentAimDirection = ResolveAimDirection(origin, currentAimPoint);

        if (weapon != null)
        {
            weapon.SetAimTarget(currentAimPoint, hasAimTarget);
            weapon.SetAimDirection(currentAimDirection);
        }

        RotateCharacter(currentAimDirection);
        RotateWeaponPivot(currentAimDirection);
        UpdateReticle();
    }

    public Vector3 GetAimDirectionFrom(Vector3 worldOrigin)
    {
        return ResolveAimDirection(worldOrigin, ResolveAimPoint(worldOrigin, out _));
    }

    private void ResolveReferences()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (weapon == null)
        {
            weapon = GetComponentInChildren<InkWeapon>();
        }

        if (firePoint == null && weapon != null)
        {
            firePoint = weapon.FirePoint;
        }

        if (characterRoot == null)
        {
            characterRoot = transform;
        }

        if (weaponPivot == null)
        {
            weaponPivot = firePoint;
        }

        if (ignoredRoot == null)
        {
            ignoredRoot = transform;
        }

        if (reticleUI == null)
        {
            reticleUI = FindObjectOfType<AimReticleUI>();
        }

        if (reticleUI == null && autoCreateReticle)
        {
            reticleUI = AimReticleUI.CreateRuntimeReticle();
        }
    }

    private Vector3 GetAimOrigin()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        if (weapon != null)
        {
            return weapon.transform.position;
        }

        return transform.position + Vector3.up;
    }

    private Vector3 ResolveAimPoint(Vector3 origin, out bool foundTarget)
    {
        foundTarget = false;

        if (aimCamera == null)
        {
            return origin + transform.forward * maxAimDistance;
        }

        Ray ray = GetAimRay();
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            aimHits,
            maxAimDistance,
            aimLayers,
            QueryTriggerInteraction.Ignore);

        int bestHitIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = aimHits[i];

            if (IsIgnoredHit(hit.transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHitIndex = i;
            }
        }

        if (bestHitIndex >= 0)
        {
            foundTarget = true;
            return aimHits[bestHitIndex].point;
        }

        return ray.origin + ray.direction * maxAimDistance;
    }

    private Vector3 ResolveAimDirection(Vector3 origin, Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - origin;

        if (direction.sqrMagnitude >= minimumAimDistance * minimumAimDistance)
        {
            return direction.normalized;
        }

        if (aimCamera != null)
        {
            return aimCamera.transform.forward.normalized;
        }

        return transform.forward;
    }

    private Ray GetAimRay()
    {
        if (aimCamera == null)
        {
            return new Ray(transform.position + Vector3.up, transform.forward);
        }

        if (aimInputMode == AimInputMode.MousePosition)
        {
            return aimCamera.ScreenPointToRay(Input.mousePosition);
        }

        return aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    private bool IsIgnoredHit(Transform hitTransform)
    {
        if (hitTransform == null)
        {
            return true;
        }

        if (ignoredRoot != null && (hitTransform == ignoredRoot || hitTransform.IsChildOf(ignoredRoot)))
        {
            return true;
        }

        return ignoreProjectiles && hitTransform.GetComponentInParent<InkProjectile>() != null;
    }

    private void RotateCharacter(Vector3 aimDirection)
    {
        if (!rotateCharacterToAim || characterRoot == null)
        {
            return;
        }

        Vector3 flatDirection = aimDirection;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
        characterRoot.rotation = Quaternion.RotateTowards(
            characterRoot.rotation,
            targetRotation,
            characterTurnSpeed * Time.deltaTime);
    }

    private void RotateWeaponPivot(Vector3 aimDirection)
    {
        if (!rotateWeaponPivotToAim || weaponPivot == null || aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        weaponPivot.rotation = Quaternion.RotateTowards(
            weaponPivot.rotation,
            targetRotation,
            weaponTurnSpeed * Time.deltaTime);
    }

    private void UpdateReticle()
    {
        if (reticleUI != null)
        {
            reticleUI.SetState(hasAimTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugAimRay)
        {
            return;
        }

        Gizmos.color = debugRayColor;
        Vector3 origin = Application.isPlaying ? GetAimOrigin() : transform.position + Vector3.up;
        Vector3 direction = Application.isPlaying ? currentAimDirection : transform.forward;

        Gizmos.DrawRay(origin, direction * 5f);
        Gizmos.DrawWireSphere(Application.isPlaying ? currentAimPoint : origin + direction * 5f, 0.15f);
    }
}
