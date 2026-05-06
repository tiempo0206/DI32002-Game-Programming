using UnityEngine;

/// <summary>
/// Marks level geometry that should block scoring cells inside a PaintableArea.
/// This keeps cover, platforms, and boundary rails from counting as paintable territory.
/// </summary>
[DisallowMultipleComponent]
public class PaintBlocker : MonoBehaviour
{
    [Header("Scoring Block")]
    [SerializeField] private bool blocksScoring = true;
    [SerializeField, Min(0f)] private float boundsPadding = 0.05f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.15f, 0.05f, 0.18f);

    public bool BlocksScoring => blocksScoring;
    public float BoundsPadding => boundsPadding;

    public bool TryGetWorldBounds(out Bounds bounds)
    {
        Collider blockerCollider = GetComponent<Collider>();

        if (blockerCollider != null)
        {
            bounds = blockerCollider.bounds;
            return true;
        }

        Renderer blockerRenderer = GetComponent<Renderer>();

        if (blockerRenderer != null)
        {
            bounds = blockerRenderer.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    public void Configure(bool shouldBlockScoring, float padding)
    {
        blocksScoring = shouldBlockScoring;
        boundsPadding = Mathf.Max(0f, padding);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !blocksScoring || !TryGetWorldBounds(out Bounds bounds))
        {
            return;
        }

        Bounds paddedBounds = bounds;
        paddedBounds.Expand(new Vector3(boundsPadding * 2f, 0.02f, boundsPadding * 2f));

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(paddedBounds.center, paddedBounds.size);
    }
}
