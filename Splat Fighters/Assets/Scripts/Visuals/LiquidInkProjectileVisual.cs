using UnityEngine;

/// <summary>
/// Adds glossy material properties and a short fluid trail to ink projectiles.
/// </summary>
[DisallowMultipleComponent]
public sealed class LiquidInkProjectileVisual : MonoBehaviour
{
    private const string CoreMaterialName = "MAT_LiquidInkProjectile_Runtime";
    private const string TrailMaterialName = "MAT_LiquidInkTrail_Runtime";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int MetallicId = Shader.PropertyToID("_Metallic");

    private static Material sharedCoreMaterial;
    private static Material sharedTrailMaterial;

    private MaterialPropertyBlock propertyBlock;
    private TrailRenderer trailRenderer;
    private Renderer[] renderers;
    private Color teamColor;
    private float baseTrailWidth;
    private float pulseSeed;
    private bool configured;

    private void Awake()
    {
        EnsurePropertyBlock();
    }

    public void Configure(Team team, float paintRadius)
    {
        teamColor = TeamVisualPalette.GetColor(team, 1f);
        pulseSeed = Random.value * Mathf.PI * 2f;
        renderers = GetComponentsInChildren<Renderer>();

        ApplyCoreMaterial();
        ConfigureTrail(paintRadius);
        configured = true;
    }

    private void Update()
    {
        if (!configured || trailRenderer == null)
        {
            return;
        }

        trailRenderer.widthMultiplier = baseTrailWidth * (0.94f + Mathf.Sin(Time.time * 18f + pulseSeed) * 0.06f);
    }

    private void ApplyCoreMaterial()
    {
        if (renderers == null)
        {
            return;
        }

        Color rimColor = Color.Lerp(teamColor, Color.white, 0.35f);
        EnsurePropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || renderer is TrailRenderer)
            {
                continue;
            }

            renderer.sharedMaterial = GetCoreMaterial();
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, teamColor);
            propertyBlock.SetColor(ColorId, teamColor);
            propertyBlock.SetColor(EmissionColorId, rimColor * 0.18f);
            propertyBlock.SetFloat(SmoothnessId, 0.92f);
            propertyBlock.SetFloat(MetallicId, 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ConfigureTrail(float paintRadius)
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }

        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        baseTrailWidth = Mathf.Clamp(paintRadius * 0.1f, 0.12f, 0.3f);
        Color endColor = teamColor;
        endColor.a = 0f;

        trailRenderer.time = 0.18f;
        trailRenderer.minVertexDistance = 0.025f;
        trailRenderer.autodestruct = false;
        trailRenderer.emitting = true;
        trailRenderer.numCornerVertices = 4;
        trailRenderer.numCapVertices = 4;
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.widthMultiplier = baseTrailWidth;
        trailRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        Color startColor = teamColor;
        startColor.a = 0.82f;
        trailRenderer.startColor = startColor;
        trailRenderer.endColor = endColor;
        trailRenderer.sharedMaterial = GetTrailMaterial();
    }

    private static Material GetCoreMaterial()
    {
        if (sharedCoreMaterial != null)
        {
            return sharedCoreMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        sharedCoreMaterial = new Material(shader);
        sharedCoreMaterial.name = CoreMaterialName;

        if (sharedCoreMaterial.HasProperty(SmoothnessId))
        {
            sharedCoreMaterial.SetFloat(SmoothnessId, 0.92f);
        }

        if (sharedCoreMaterial.HasProperty(MetallicId))
        {
            sharedCoreMaterial.SetFloat(MetallicId, 0f);
        }

        return sharedCoreMaterial;
    }

    private static Material GetTrailMaterial()
    {
        if (sharedTrailMaterial != null)
        {
            return sharedTrailMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        sharedTrailMaterial = new Material(shader);
        sharedTrailMaterial.name = TrailMaterialName;
        return sharedTrailMaterial;
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
}
