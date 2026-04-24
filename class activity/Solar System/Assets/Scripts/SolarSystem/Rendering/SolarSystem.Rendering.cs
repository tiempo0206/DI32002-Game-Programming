using UnityEngine;

public sealed partial class SolarSystem
{
    private void ConfigureRenderQuality()
    {
        QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, NormalizeAntiAliasing(antiAliasingSamples));
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 2f);
    }

    private int NormalizeAntiAliasing(int samples)
    {
        if (samples >= 8)
        {
            return 8;
        }

        if (samples >= 4)
        {
            return 4;
        }

        if (samples >= 2)
        {
            return 2;
        }

        return 0;
    }

    private void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.016f, 0.018f, 0.024f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.003f, 0.004f, 0.008f);
        RenderSettings.fogDensity = 0.0015f;

        Light[] lights = FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                lights[i].intensity = 0.12f;
                lights[i].color = new Color(0.72f, 0.82f, 1f);
            }
        }
    }

    private void SetupCamera()
    {
        sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = new Color(0.002f, 0.003f, 0.008f);
        sceneCamera.nearClipPlane = 0.03f;
        sceneCamera.farClipPlane = unitsPerAU * 80f;
        sceneCamera.fieldOfView = 54f;
        sceneCamera.allowHDR = true;
        sceneCamera.allowMSAA = true;

        cameraController = sceneCamera.GetComponent<SolarSystemCameraController>();
        if (cameraController == null)
        {
            cameraController = sceneCamera.gameObject.AddComponent<SolarSystemCameraController>();
        }

        cameraController.Initialize(transform, unitsPerAU * 20f, unitsPerAU * 2f, unitsPerAU * 45f);
    }

    private Material CreateStandardMaterial(Color color, Texture2D texture, bool emissive)
    {
        Material material = new Material(FindShader("Standard"));
        material.color = color;
        if (texture != null)
        {
            material.mainTexture = texture;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissive ? new Color(1f, 0.56f, 0.08f) * 2.2f : Color.black);
        return material;
    }

    private Material CreateUnlitMaterial(Color color)
    {
        Material material = new Material(FindShader("Unlit/Transparent"));
        material.color = color;
        return material;
    }

    private Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    private Texture2D CreatePlanetTexture(PlanetDefinition definition)
    {
        int width = Mathf.Clamp(Mathf.NextPowerOfTwo(planetTextureWidth), 256, 2048);
        int height = Mathf.Clamp(Mathf.NextPowerOfTwo(planetTextureHeight), 128, 1024);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;

        float seed = StableSeed(definition.Name);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)width;
                Color pixel = SamplePlanetColor(definition, u, v, seed);
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    private Color SamplePlanetColor(PlanetDefinition definition, float u, float v, float seed)
    {
        float latitude = Mathf.Abs(v - 0.5f) * 2f;
        float n1 = FractalNoise(u * 5.5f + seed, v * 3.2f + seed);
        float n2 = FractalNoise(u * 17f + seed * 0.37f, v * 9f + seed * 0.17f);

        if (definition.Style == PlanetStyle.Sun)
        {
            float flare = FractalNoise(u * 10f + seed, v * 10f + seed);
            return Color.Lerp(new Color(1f, 0.42f, 0.03f), new Color(1f, 0.92f, 0.18f), Mathf.Clamp01(0.25f + flare * 0.95f));
        }

        if (definition.Style == PlanetStyle.Earth)
        {
            Color ocean = Color.Lerp(new Color(0.02f, 0.13f, 0.38f), new Color(0.05f, 0.36f, 0.78f), n2);
            Color land = Color.Lerp(new Color(0.10f, 0.38f, 0.17f), new Color(0.60f, 0.47f, 0.25f), n2);
            Color color = n1 > 0.53f ? land : ocean;
            if (latitude > 0.82f)
            {
                color = Color.Lerp(color, Color.white, Mathf.InverseLerp(0.82f, 1f, latitude));
            }

            float cloud = FractalNoise(u * 18f + seed * 1.7f, v * 8f + seed * 2.1f);
            if (cloud > 0.67f)
            {
                color = Color.Lerp(color, Color.white, 0.42f);
            }

            return color;
        }

        if (definition.Style == PlanetStyle.GasGiant)
        {
            float bands = Mathf.Sin(v * Mathf.PI * 28f + n1 * 3.5f) * 0.5f + 0.5f;
            Color darker = ScaleColor(definition.BaseColor, 0.62f);
            Color lighter = ScaleColor(definition.BaseColor, 1.28f);
            Color color = Color.Lerp(darker, lighter, bands);

            if (definition.Name == "Jupiter")
            {
                float storm = Mathf.Exp(-Mathf.Pow((u - 0.68f) * 12f, 2f) - Mathf.Pow((v - 0.42f) * 18f, 2f));
                color = Color.Lerp(color, new Color(0.78f, 0.22f, 0.12f), storm * 0.8f);
            }

            return color;
        }

        if (definition.Style == PlanetStyle.IceGiant)
        {
            Color color = Color.Lerp(ScaleColor(definition.BaseColor, 0.72f), ScaleColor(definition.BaseColor, 1.22f), n1);
            return Color.Lerp(color, Color.white, Mathf.Clamp01(0.06f + latitude * 0.12f));
        }

        Color rocky = Color.Lerp(ScaleColor(definition.BaseColor, 0.58f), ScaleColor(definition.BaseColor, 1.24f), n1);
        return Color.Lerp(rocky, new Color(0.1f, 0.09f, 0.08f), n2 * 0.18f);
    }

    private float FractalNoise(float x, float y)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;

        for (int i = 0; i < 4; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            frequency *= 2.03f;
            amplitude *= 0.5f;
        }

        return Mathf.Clamp01(value);
    }

    private float StableSeed(string text)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return Mathf.Abs(hash % 10000) * 0.0137f;
        }
    }

    private Color ScaleColor(Color color, float scale)
    {
        return new Color(Mathf.Clamp01(color.r * scale), Mathf.Clamp01(color.g * scale), Mathf.Clamp01(color.b * scale), color.a);
    }

    private Shader FindShader(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return shader;
    }

    private void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyUnityObject(collider);
        }
    }

    private void DestroyUnityObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
