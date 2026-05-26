using UnityEngine;

/// <summary>
/// Applies the shared team palette to character renderers so player and bot colors stay consistent.
/// </summary>
[DisallowMultipleComponent]
public class TeamVisualBinder : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int Color01Id = Shader.PropertyToID("_Color01");
    private static readonly int Color02Id = Shader.PropertyToID("_Color02");
    private static readonly int Color03Id = Shader.PropertyToID("_Color03");

    [Header("Team")]
    [SerializeField] private Team team = Team.None;

    [Header("Renderer Targets")]
    [SerializeField] private bool includeChildRenderers = true;
    [SerializeField] private Renderer[] targetRenderers = new Renderer[0];

    [Header("Material Overrides")]
    [SerializeField] private bool useMaterialOverride = true;
    [SerializeField] private Material teamAMaterial = null;
    [SerializeField] private Material teamBMaterial = null;

    [Header("Runtime")]
    [SerializeField] private bool applyOnAwake = true;

    private MaterialPropertyBlock propertyBlock;

    public Team Team => team;

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyVisuals();
        }
    }

    private void OnValidate()
    {
        RefreshRenderersIfNeeded();
        ApplyVisuals();
    }

    public void Configure(Team newTeam, Material newTeamAMaterial, Material newTeamBMaterial)
    {
        team = newTeam;
        teamAMaterial = newTeamAMaterial;
        teamBMaterial = newTeamBMaterial;
        RefreshRenderers();
        ApplyVisuals();
    }

    [ContextMenu("Refresh Team Visuals")]
    public void ApplyVisuals()
    {
        RefreshRenderersIfNeeded();

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        Material overrideMaterial = GetOverrideMaterial();
        Color teamColor = TeamVisualPalette.GetColor(team);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer = targetRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            if (useMaterialOverride && overrideMaterial != null)
            {
                targetRenderer.sharedMaterial = overrideMaterial;
            }

            ApplyColorPropertyBlock(targetRenderer, teamColor);
        }
    }

    [ContextMenu("Refresh Renderer Targets")]
    public void RefreshRenderers()
    {
        targetRenderers = includeChildRenderers
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
    }

    private void RefreshRenderersIfNeeded()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            RefreshRenderers();
        }
    }

    private Material GetOverrideMaterial()
    {
        switch (team)
        {
            case Team.TeamA:
                return teamAMaterial;
            case Team.TeamB:
                return teamBMaterial;
            default:
                return null;
        }
    }

    private void ApplyColorPropertyBlock(Renderer targetRenderer, Color color)
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetColor(Color01Id, color);
        propertyBlock.SetColor(Color02Id, Color.Lerp(color, Color.white, 0.24f));
        propertyBlock.SetColor(Color03Id, Color.Lerp(color, Color.black, 0.35f));
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
