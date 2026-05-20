using UnityEngine;

/// <summary>
/// Runtime-only ink impact feedback that creates a short particle burst without imported art assets.
/// </summary>
public sealed class InkSplatterVfx : MonoBehaviour
{
    private const float MinNormalMagnitude = 0.0001f;
    private const int MaxActiveSplatterInstances = 14;
    private static int activeSplatterInstances;

    [SerializeField] private ParticleSystem splatterParticles = null;
    [SerializeField, Min(0.05f)] private float cleanupDelay = 0.9f;

    public static InkSplatterVfx Spawn(Vector3 position, Vector3 normal, Team team, bool paintableImpact, float paintRadius)
    {
        if (activeSplatterInstances >= MaxActiveSplatterInstances)
        {
            return null;
        }

        GameObject root = new GameObject($"InkSplatterVfx_{TeamVisualPalette.GetLabel(team).Replace(" ", string.Empty)}");
        InkSplatterVfx splatter = root.AddComponent<InkSplatterVfx>();
        splatter.Configure(position, normal, team, paintableImpact, paintRadius);
        return splatter;
    }

    public void Configure(Vector3 position, Vector3 normal, Team team, bool paintableImpact, float paintRadius)
    {
        Vector3 safeNormal = normal.sqrMagnitude > MinNormalMagnitude ? normal.normalized : Vector3.up;
        transform.position = position + safeNormal * 0.04f;
        transform.rotation = CreateSurfaceRotation(safeNormal);
        activeSplatterInstances++;

        splatterParticles = gameObject.AddComponent<ParticleSystem>();
        ConfigureParticles(splatterParticles, team, paintableImpact, paintRadius);
        splatterParticles.Play();
        Destroy(gameObject, cleanupDelay);
    }

    private void OnDestroy()
    {
        activeSplatterInstances = Mathf.Max(0, activeSplatterInstances - 1);
    }

    private void ConfigureParticles(ParticleSystem particles, Team team, bool paintableImpact, float paintRadius)
    {
        float safeRadius = Mathf.Max(0.25f, paintRadius);
        Color baseColor = paintableImpact
            ? TeamVisualPalette.GetColor(team, 0.95f)
            : new Color(0.82f, 0.86f, 0.9f, 0.45f);
        Color brightColor = Color.Lerp(baseColor, Color.white, paintableImpact ? 0.2f : 0.45f);
        brightColor.a = baseColor.a;

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.24f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(safeRadius * 1.1f, safeRadius * 2.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(safeRadius * 0.08f, safeRadius * 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(baseColor, brightColor);
        main.gravityModifier = 0.18f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(safeRadius * 8f), 8, 20))
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 58f;
        shape.radius = Mathf.Clamp(safeRadius * 0.1f, 0.06f, 0.24f);
        shape.rotation = Vector3.zero;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 0.62f),
            new Keyframe(1f, 0.08f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(brightColor, 0.22f),
                new GradientColorKey(baseColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(baseColor.a, 0f),
                new GradientAlphaKey(baseColor.a * 0.82f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();

        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingFudge = 0.1f;
        }
    }

    private static Quaternion CreateSurfaceRotation(Vector3 normal)
    {
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        return Quaternion.LookRotation(normal, up);
    }
}
