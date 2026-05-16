using UnityEngine;

/// <summary>
/// Spends a ready special meter on one larger paint burst.
/// </summary>
[DisallowMultipleComponent]
public class SpecialPaintBurst : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpecialMeter specialMeter = null;
    [SerializeField] private AimController aimController = null;

    [Header("Burst")]
    [SerializeField] private Team team = Team.TeamA;
    [SerializeField, Min(0.1f)] private float burstPaintRadius = 4.25f;
    [SerializeField, Min(0.1f)] private float fallbackDistance = 4.5f;
    [SerializeField] private KeyCode activationKey = KeyCode.Q;
    [SerializeField] private bool requireMatchPlaying = true;

    [Header("Debug")]
    [SerializeField] private bool logActivation = false;

    public KeyCode ActivationKey => activationKey;
    public float BurstPaintRadius => burstPaintRadius;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
        {
            TryActivate();
        }
    }

    public bool TryActivate()
    {
        ResolveReferences();

        if (requireMatchPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.MatchState.Playing)
        {
            return false;
        }

        if (specialMeter == null || !specialMeter.IsReady || PaintManager.Instance == null)
        {
            return false;
        }

        Vector3 paintPoint = ResolvePaintPoint();

        if (!PaintManager.Instance.CanPaintAtWorldPosition(paintPoint))
        {
            return false;
        }

        int changedCells = PaintManager.Instance.PaintAtWorldPosition(paintPoint, burstPaintRadius, team);

        if (changedCells <= 0)
        {
            return false;
        }

        specialMeter.ConsumeReadyCharge();

        if (logActivation)
        {
            Debug.Log($"Special paint burst changed {changedCells} cells for {team}.", this);
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (specialMeter == null)
        {
            specialMeter = GetComponentInChildren<SpecialMeter>();
        }

        if (aimController == null)
        {
            aimController = GetComponentInChildren<AimController>();
        }

        if (specialMeter != null)
        {
            team = specialMeter.Team;
        }
    }

    private Vector3 ResolvePaintPoint()
    {
        if (aimController != null)
        {
            aimController.RefreshAimNow();

            if (aimController.HasAimTarget)
            {
                return aimController.CurrentAimPoint;
            }

            Vector3 aimFallback = transform.position + aimController.CurrentAimDirection.normalized * fallbackDistance;
            aimFallback.y = transform.position.y;
            return aimFallback;
        }

        Vector3 fallback = transform.position + transform.forward * fallbackDistance;
        fallback.y = transform.position.y;
        return fallback;
    }
}
