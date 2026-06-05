using UnityEngine;

/// <summary>
/// Simple team spawn marker for players and future bots.
/// The transform position is the character root position, not the floor pad position.
/// </summary>
[DisallowMultipleComponent]
public class SpawnPoint : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int Color01Id = Shader.PropertyToID("_Color01");
    private static readonly int Color02Id = Shader.PropertyToID("_Color02");
    private static readonly int Color03Id = Shader.PropertyToID("_Color03");

    [Header("Team")]
    [SerializeField] private Team team = Team.TeamA;
    [SerializeField] private bool defaultForTeam = true;

    [Header("Spawn Pad Visuals")]
    [SerializeField] private bool tintChildRenderers = true;
    [SerializeField] private Renderer[] tintRenderers = new Renderer[0];
    [SerializeField] private bool refreshRuntimeColor = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField, Min(0.05f)] private float gizmoRadius = 0.45f;
    [SerializeField] private Color teamAColor = TeamVisualPalette.TeamAGizmoColor;
    [SerializeField] private Color teamBColor = TeamVisualPalette.TeamBGizmoColor;
    [SerializeField] private Color neutralColor = TeamVisualPalette.NeutralColor;

    public Team Team => team;
    public bool DefaultForTeam => defaultForTeam;
    public Vector3 SpawnPosition => transform.position;
    public Quaternion SpawnRotation => transform.rotation;

    private MaterialPropertyBlock propertyBlock;
    private Color lastAppliedColor = new Color(-1f, -1f, -1f, -1f);

    private void Awake()
    {
        RefreshTintRenderersIfNeeded();
        ApplyTeamColor(true);
    }

    private void OnEnable()
    {
        RefreshTintRenderersIfNeeded();
        ApplyTeamColor(true);
    }

    private void LateUpdate()
    {
        if (refreshRuntimeColor)
        {
            ApplyTeamColor(false);
        }
    }

    private void OnValidate()
    {
        RefreshTintRenderersIfNeeded();
        ApplyTeamColor(true);
    }

    public void Configure(Team newTeam, bool newDefaultForTeam)
    {
        team = newTeam;
        defaultForTeam = newDefaultForTeam;
        ApplyTeamColor(true);
    }

    [ContextMenu("Refresh Spawn Pad Color")]
    public void RefreshSpawnPadColor()
    {
        RefreshTintRenderers();
        ApplyTeamColor(true);
    }

    private void RefreshTintRenderersIfNeeded()
    {
        if (tintRenderers == null || tintRenderers.Length == 0)
        {
            RefreshTintRenderers();
        }
    }

    private void RefreshTintRenderers()
    {
        tintRenderers = tintChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }

    private void ApplyTeamColor(bool force)
    {
        RefreshTintRenderersIfNeeded();

        if (tintRenderers == null || tintRenderers.Length == 0)
        {
            return;
        }

        Color teamColor = TeamVisualPalette.GetColor(team);

        if (!force && ColorsApproximatelyEqual(teamColor, lastAppliedColor))
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < tintRenderers.Length; i++)
        {
            Renderer targetRenderer = tintRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, teamColor);
            propertyBlock.SetColor(ColorId, teamColor);
            propertyBlock.SetColor(Color01Id, teamColor);
            propertyBlock.SetColor(Color02Id, Color.Lerp(teamColor, Color.white, 0.24f));
            propertyBlock.SetColor(Color03Id, Color.Lerp(teamColor, Color.black, 0.35f));
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        lastAppliedColor = teamColor;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = GetGizmoColor();
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.DrawRay(transform.position, transform.forward * (gizmoRadius * 2.5f));
    }

    private Color GetGizmoColor()
    {
        switch (team)
        {
            case Team.TeamA:
                return TeamVisualPalette.GetColor(Team.TeamA, teamAColor.a);
            case Team.TeamB:
                return TeamVisualPalette.GetColor(Team.TeamB, teamBColor.a);
            default:
                return neutralColor;
        }
    }

    private static bool ColorsApproximatelyEqual(Color a, Color b)
    {
        return Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);
    }
}
