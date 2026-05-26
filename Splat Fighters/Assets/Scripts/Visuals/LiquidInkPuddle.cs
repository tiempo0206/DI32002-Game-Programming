using UnityEngine;

/// <summary>
/// Short-lived glossy blob used to make ink impacts read as liquid.
/// </summary>
[DisallowMultipleComponent]
public sealed class LiquidInkPuddle : MonoBehaviour
{
    private const string ShaderName = "Splat Fighters/Liquid Ink Blob";
    private const float SurfaceOffset = 0.035f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
    private static readonly int StartTimeId = Shader.PropertyToID("_StartTime");
    private static readonly int LifetimeId = Shader.PropertyToID("_Lifetime");
    private static readonly int GlossStrengthId = Shader.PropertyToID("_GlossStrength");

    private static Material sharedBlobMaterial;

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 startScale;
    private float spawnTime;
    private float lifetime;
    private Color baseColor;
    private Color rimColor;

    public static void Spawn(Vector3 position, Vector3 normal, Team team, bool paintableImpact, float paintRadius)
    {
        Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Quad);
        puddle.name = $"LiquidInkPuddle_{TeamVisualPalette.GetLabel(team).Replace(" ", string.Empty)}";
        puddle.transform.position = position + safeNormal * SurfaceOffset;
        puddle.transform.rotation = CreateSurfaceRotation(safeNormal);

        Collider collider = puddle.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        float size = Mathf.Clamp(paintRadius * (paintableImpact ? 0.88f : 0.32f), 0.35f, 4.2f);
        puddle.transform.localScale = new Vector3(size, size, 1f);

        LiquidInkPuddle liquidPuddle = puddle.AddComponent<LiquidInkPuddle>();
        liquidPuddle.Configure(team, paintableImpact, paintRadius);
    }

    private void Configure(Team team, bool paintableImpact, float paintRadius)
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        spawnTime = Time.time;
        lifetime = Mathf.Clamp(0.34f + paintRadius * 0.08f, 0.42f, 0.9f);
        startScale = transform.localScale;
        baseColor = paintableImpact
            ? TeamVisualPalette.GetColor(team, 0.78f)
            : new Color(0.82f, 0.86f, 0.9f, 0.36f);
        rimColor = Color.Lerp(baseColor, Color.white, paintableImpact ? 0.5f : 0.75f);
        rimColor.a = 0.52f;

        if (targetRenderer != null)
        {
            targetRenderer.sharedMaterial = GetBlobMaterial();
            targetRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            targetRenderer.receiveShadows = false;
        }

        ApplyProperties(1f);
        Destroy(gameObject, lifetime + 0.05f);
    }

    private void Update()
    {
        if (targetRenderer == null)
        {
            return;
        }

        float age = Mathf.Clamp01((Time.time - spawnTime) / Mathf.Max(lifetime, 0.001f));
        float grow = 1f + age * 0.18f;
        transform.localScale = startScale * grow;
        ApplyProperties(1f - age);
    }

    private void ApplyProperties(float fade)
    {
        Color fadedBase = baseColor;
        fadedBase.a *= fade;

        propertyBlock.Clear();
        propertyBlock.SetColor(BaseColorId, fadedBase);
        propertyBlock.SetColor(RimColorId, rimColor);
        propertyBlock.SetFloat(StartTimeId, spawnTime);
        propertyBlock.SetFloat(LifetimeId, lifetime);
        propertyBlock.SetFloat(GlossStrengthId, 0.48f);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private static Material GetBlobMaterial()
    {
        if (sharedBlobMaterial != null)
        {
            return sharedBlobMaterial;
        }

        Shader shader = Shader.Find(ShaderName);

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        sharedBlobMaterial = new Material(shader);
        sharedBlobMaterial.name = "MAT_LiquidInkBlob_Runtime";
        return sharedBlobMaterial;
    }

    private static Quaternion CreateSurfaceRotation(Vector3 normal)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(normal, up);
    }
}
