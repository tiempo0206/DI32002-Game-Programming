using UnityEngine;

/// <summary>
/// Optional close-range paint tool that paints a continuous swath instead of firing projectiles.
/// </summary>
[DisallowMultipleComponent]
public class RollerPaintTool : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private Team team = Team.TeamA;

    [Header("Input")]
    [SerializeField] private KeyCode paintKey = KeyCode.Mouse0;
    [SerializeField] private bool requireInput = true;
    [SerializeField] private bool requireMatchPlaying = true;

    [Header("Paint Swath")]
    [SerializeField] private Transform paintOrigin = null;
    [SerializeField, Min(0.05f)] private float paintInterval = 0.08f;
    [SerializeField, Min(0.1f)] private float paintRadius = 1.05f;
    [SerializeField, Min(0f)] private float forwardOffset = 1.15f;
    [SerializeField, Min(0f)] private float halfWidth = 0.65f;
    [SerializeField, Range(1, 5)] private int swathSamples = 3;
    [SerializeField, Min(0f)] private float fallbackPaintPlaneY = 0f;
    [SerializeField] private LayerMask groundProbeLayers = ~0;

    [Header("Movement Gate")]
    [SerializeField] private bool requireMovementForTrail = true;
    [SerializeField, Min(0f)] private float minMoveDistance = 0.06f;

    private Vector3 lastPaintOriginPosition;
    private float nextPaintTime;

    public float PaintRadius => paintRadius;
    public float HalfWidth => halfWidth;
    public int SwathSamples => swathSamples;

    private void Awake()
    {
        if (paintOrigin == null)
        {
            paintOrigin = transform;
        }

        lastPaintOriginPosition = paintOrigin.position;
    }

    private void OnEnable()
    {
        if (paintOrigin == null)
        {
            paintOrigin = transform;
        }

        lastPaintOriginPosition = paintOrigin.position;
        nextPaintTime = 0f;
    }

    private void Update()
    {
        if (!CanAttemptPaint())
        {
            return;
        }

        PaintCurrentSwath();
        lastPaintOriginPosition = paintOrigin.position;
        nextPaintTime = Time.time + paintInterval;
    }

    public int PaintCurrentSwath()
    {
        if (PaintManager.Instance == null)
        {
            return 0;
        }

        int totalChangedCells = 0;
        int sampleCount = Mathf.Max(1, swathSamples);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount == 1 ? 0.5f : i / (sampleCount - 1f);
            float sideOffset = Mathf.Lerp(-halfWidth, halfWidth, t);
            Vector3 paintPoint = ResolvePaintPoint(sideOffset);

            if (!PaintManager.Instance.CanPaintAtWorldPosition(paintPoint))
            {
                continue;
            }

            totalChangedCells += PaintManager.Instance.PaintAtWorldPosition(paintPoint, paintRadius, team);
        }

        return totalChangedCells;
    }

    private bool CanAttemptPaint()
    {
        if (paintOrigin == null || PaintManager.Instance == null || Time.time < nextPaintTime)
        {
            return false;
        }

        if (requireMatchPlaying && GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.MatchState.Playing)
        {
            return false;
        }

        if (requireInput && !Input.GetKey(paintKey))
        {
            return false;
        }

        if (!requireMovementForTrail)
        {
            return true;
        }

        Vector3 movement = paintOrigin.position - lastPaintOriginPosition;
        movement.y = 0f;
        return movement.sqrMagnitude >= minMoveDistance * minMoveDistance;
    }

    private Vector3 ResolvePaintPoint(float sideOffset)
    {
        Vector3 forward = paintOrigin.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward = forward.sqrMagnitude <= 0.0001f ? Vector3.forward : forward.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 basePoint = paintOrigin.position + forward * forwardOffset + right * sideOffset;
        Vector3 rayOrigin = basePoint + Vector3.up * 2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5f, groundProbeLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        basePoint.y = fallbackPaintPlaneY;
        return basePoint;
    }
}
