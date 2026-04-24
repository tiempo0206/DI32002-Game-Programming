using UnityEngine;

public sealed partial class SolarSystem
{
    private void BuildSystem()
    {
        ClearGeneratedObjects();

        bodies.Clear();
        asteroids.Clear();
        orbitLineObjects.Clear();
        selectableBodies.Clear();
        selectableByCollider.Clear();
        selectedBody = null;
        simulatedDays = 0f;

        generatedRoot = new GameObject("Generated Solar System").transform;
        generatedRoot.SetParent(transform, false);

        orbitMaterial = CreateUnlitMaterial(new Color(1f, 1f, 1f, 0.18f));
        asteroidMaterial = CreateStandardMaterial(new Color(0.46f, 0.42f, 0.36f), null, false);

        CreateStarField();
        CreateSun();
        CreatePlanets();
        CreateAsteroidBelt();
        SetupLighting();
        SetupCamera();
        SetupAudio();
    }

    private void ClearGeneratedObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Generated Solar System")
            {
                DestroyUnityObject(child.gameObject);
            }
        }
    }

    private void CreateSun()
    {
        GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sun.name = "Sun";
        sun.transform.SetParent(generatedRoot, false);
        sun.transform.localScale = Vector3.one * sunVisualRadius * 2f;

        PlanetDefinition sunDefinition = new PlanetDefinition
        {
            Name = "Sun",
            BaseColor = new Color(1f, 0.72f, 0.18f),
            Style = PlanetStyle.Sun,
            KidPrompt = "The Sun is the glowing star in the middle.",
            KidFact = "It gives light and warmth to every planet in this little system.",
            HighlightColor = new Color(1f, 0.78f, 0.12f),
            FocusDistanceMultiplier = 2.25f,
            TonePitch = 0.78f
        };

        Renderer renderer = sun.GetComponent<Renderer>();
        Material material = CreateStandardMaterial(Color.white, CreatePlanetTexture(sunDefinition), true);
        renderer.sharedMaterial = material;

        Light pointLight = sun.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.88f, 0.62f);
        pointLight.intensity = 5.5f;
        pointLight.range = unitsPerAU * 36f;
        pointLight.shadows = LightShadows.Soft;

        RegisterSelectable(
            sunDefinition.Name,
            sunDefinition.KidPrompt,
            sunDefinition.KidFact,
            sun.transform,
            sun.transform,
            renderer,
            material,
            sunDefinition.HighlightColor,
            Mathf.Max(3.4f, sunVisualRadius * sunDefinition.FocusDistanceMultiplier),
            sunDefinition.TonePitch);
    }

    private void CreatePlanets()
    {
        for (int i = 0; i < PlanetDefinitions.Length; i++)
        {
            OrbitBody body = CreatePlanet(PlanetDefinitions[i]);
            bodies.Add(body);
        }
    }

    private OrbitBody CreatePlanet(PlanetDefinition definition)
    {
        float radius = Mathf.Max(minimumPlanetRadius, definition.RadiusEarth * planetRadiusScale);

        GameObject orbitingRootObject = new GameObject(definition.Name);
        orbitingRootObject.transform.SetParent(generatedRoot, false);

        GameObject axisRootObject = new GameObject(definition.Name + " Axis");
        axisRootObject.transform.SetParent(orbitingRootObject.transform, false);

        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = definition.Name + " Body";
        planet.transform.SetParent(axisRootObject.transform, false);
        planet.transform.localScale = Vector3.one * radius * 2f;

        Renderer renderer = planet.GetComponent<Renderer>();
        Material material = CreateStandardMaterial(Color.white, CreatePlanetTexture(definition), false);
        renderer.sharedMaterial = material;

        if (definition.HasRings)
        {
            CreateRing(
                axisRootObject.transform,
                radius * definition.RingInnerRadiusMultiplier,
                radius * definition.RingOuterRadiusMultiplier,
                definition.RingColor,
                definition.Name + " Rings");
        }

        Transform label = CreateLabel(definition.Name, orbitingRootObject.transform, radius + 0.35f, definition.BaseColor);
        GameObject orbitLine = CreateOrbitLine(definition);
        orbitLineObjects.Add(orbitLine);

        OrbitBody body = new OrbitBody
        {
            Definition = definition,
            OrbitingRoot = orbitingRootObject.transform,
            AxisRoot = axisRootObject.transform,
            BodyTransform = planet.transform,
            Renderer = renderer,
            Material = material,
            Label = label,
            VisualRadius = radius
        };

        RegisterSelectable(
            definition.Name,
            definition.KidPrompt,
            definition.KidFact,
            planet.transform,
            planet.transform,
            renderer,
            material,
            definition.HighlightColor,
            Mathf.Max(1.2f, radius * definition.FocusDistanceMultiplier),
            definition.TonePitch);

        if (definition.HasMoon)
        {
            CreateMoon(body);
        }

        return body;
    }

    private void CreateMoon(OrbitBody earth)
    {
        GameObject moonRootObject = new GameObject("Moon");
        moonRootObject.transform.SetParent(earth.OrbitingRoot, false);

        GameObject moonAxisObject = new GameObject("Moon Axis");
        moonAxisObject.transform.SetParent(moonRootObject.transform, false);

        float moonRadius = Mathf.Max(0.08f, MoonRadiusEarth * moonRadiusScale);
        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon Body";
        moon.transform.SetParent(moonAxisObject.transform, false);
        moon.transform.localScale = Vector3.one * moonRadius * 2f;

        PlanetDefinition moonDefinition = new PlanetDefinition
        {
            Name = "Moon",
            BaseColor = new Color(0.62f, 0.62f, 0.58f),
            Style = PlanetStyle.Rocky,
            KidPrompt = "The Moon is Earth's space partner.",
            KidFact = "It shines because it reflects sunlight and it helps make ocean tides.",
            HighlightColor = new Color(0.92f, 0.92f, 0.84f),
            FocusDistanceMultiplier = 8.8f,
            TonePitch = 1.42f
        };

        Renderer renderer = moon.GetComponent<Renderer>();
        Material material = CreateStandardMaterial(Color.white, CreatePlanetTexture(moonDefinition), false);
        renderer.sharedMaterial = material;

        earth.MoonOrbitingRoot = moonRootObject.transform;
        earth.MoonAxisRoot = moonAxisObject.transform;
        earth.MoonBodyTransform = moon.transform;
        earth.MoonRenderer = renderer;
        earth.MoonMaterial = material;
        earth.MoonLabel = CreateLabel("Moon", moonRootObject.transform, moonRadius + 0.16f, new Color(0.75f, 0.75f, 0.72f));

        RegisterSelectable(
            moonDefinition.Name,
            moonDefinition.KidPrompt,
            moonDefinition.KidFact,
            moon.transform,
            moon.transform,
            renderer,
            material,
            moonDefinition.HighlightColor,
            Mathf.Max(0.85f, moonRadius * moonDefinition.FocusDistanceMultiplier),
            moonDefinition.TonePitch);
    }

    private void CreateAsteroidBelt()
    {
        if (asteroidCount <= 0)
        {
            return;
        }

        GameObject beltRoot = new GameObject("Asteroid Belt");
        beltRoot.transform.SetParent(generatedRoot, false);

        Random.State previousState = Random.state;
        Random.InitState(44021);

        for (int i = 0; i < asteroidCount; i++)
        {
            float orbitAU = Random.Range(2.05f, 3.35f);
            float size = Random.Range(0.015f, 0.055f);

            GameObject asteroid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroid.name = "Asteroid";
            asteroid.transform.SetParent(beltRoot.transform, false);
            asteroid.transform.localScale = Vector3.one * size;
            RemoveCollider(asteroid);
            asteroid.GetComponent<Renderer>().sharedMaterial = asteroidMaterial;

            AsteroidBody body = new AsteroidBody
            {
                Transform = asteroid.transform,
                SemiMajorAxisAU = orbitAU,
                OrbitalPeriodDays = EarthYearDays * Mathf.Pow(orbitAU, 1.5f),
                InclinationDegrees = Random.Range(-9f, 9f),
                Eccentricity = Random.Range(0f, 0.16f),
                InitialMeanAnomalyDegrees = Random.Range(0f, 360f)
            };
            asteroids.Add(body);
        }

        Random.state = previousState;
    }

    private void CreateStarField()
    {
        if (starCount <= 0)
        {
            return;
        }

        GameObject stars = new GameObject("Star Field");
        stars.transform.SetParent(generatedRoot, false);

        ParticleSystem particleSystem = stars.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = starCount;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial();

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[starCount];
        Random.State previousState = Random.state;
        Random.InitState(92733);

        float radius = unitsPerAU * 45f;
        for (int i = 0; i < particles.Length; i++)
        {
            Color starColor = Color.Lerp(new Color(0.62f, 0.74f, 1f, 1f), new Color(1f, 0.9f, 0.72f, 1f), Random.value);
            starColor = Color.Lerp(starColor, Color.white, Random.Range(0.2f, 0.75f));

            particles[i].position = Random.onUnitSphere * radius;
            particles[i].startSize = Random.Range(0.045f, 0.18f);
            particles[i].startColor = starColor;
            particles[i].startLifetime = 999999f;
            particles[i].remainingLifetime = 999999f;
        }

        Random.state = previousState;
        particleSystem.SetParticles(particles, particles.Length);
        particleSystem.Play();
    }

    private GameObject CreateOrbitLine(PlanetDefinition definition)
    {
        GameObject orbit = new GameObject(definition.Name + " Orbit");
        orbit.transform.SetParent(generatedRoot, false);

        LineRenderer line = orbit.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = orbitSegments;
        line.widthMultiplier = Mathf.Lerp(0.01f, 0.035f, Mathf.InverseLerp(0.3f, 30f, definition.SemiMajorAxisAU));
        line.material = orbitMaterial;
        line.startColor = new Color(1f, 1f, 1f, 0.16f);
        line.endColor = new Color(1f, 1f, 1f, 0.16f);

        for (int i = 0; i < orbitSegments; i++)
        {
            float anomaly = (i / (float)orbitSegments) * 360f;
            line.SetPosition(i, ComputeOrbitPosition(definition.SemiMajorAxisAU, definition.Eccentricity, definition.InclinationDegrees, anomaly));
        }

        orbit.SetActive(showOrbitLines);
        return orbit;
    }

    private Transform CreateLabel(string text, Transform parent, float localHeight, Color color)
    {
        GameObject labelObject = new GameObject(text + " Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, localHeight, 0f);

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 128;
        textMesh.characterSize = 0.032f;
        textMesh.color = Color.Lerp(color, Color.white, 0.45f);

        labelObject.SetActive(showLabels);
        return labelObject.transform;
    }

    private void CreateRing(Transform parent, float innerRadius, float outerRadius, Color color, string name)
    {
        const int segments = 144;
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            int vertexIndex = i * 2;

            vertices[vertexIndex] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            vertices[vertexIndex + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            uvs[vertexIndex] = new Vector2(i / (float)segments, 0f);
            uvs[vertexIndex + 1] = new Vector2(i / (float)segments, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int vertexIndex = i * 2;
            int triangleIndex = i * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 3;
            triangles[triangleIndex + 5] = vertexIndex + 2;
        }

        mesh.name = name + " Mesh";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);

        MeshFilter filter = ring.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateUnlitMaterial(color);
    }
}
