using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed partial class SolarSystem : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField] private float daysPerSecond = 24f;
    [SerializeField] private bool paused;
    [SerializeField] private bool showLabels = true;
    [SerializeField] private bool showOrbitLines = true;

    [Header("Visual Scale")]
    [SerializeField] private float unitsPerAU = 8f;
    [SerializeField] private float planetRadiusScale = 0.18f;
    [SerializeField] private float moonRadiusScale = 0.12f;
    [SerializeField] private float minimumPlanetRadius = 0.18f;
    [SerializeField] private float sunVisualRadius = 2.8f;
    [SerializeField] private float earthMoonOrbitRadius = 0.82f;

    [Header("Scene Detail")]
    [SerializeField, Range(0, 1200)] private int asteroidCount = 300;
    [SerializeField, Range(0, 2000)] private int starCount = 900;
    [SerializeField, Range(32, 256)] private int orbitSegments = 160;

    [Header("Rendering Quality")]
    [SerializeField, Range(256, 2048)] private int planetTextureWidth = 1024;
    [SerializeField, Range(128, 1024)] private int planetTextureHeight = 512;
    [SerializeField] private int antiAliasingSamples = 4;

    [Header("Interaction")]
    [SerializeField] private bool autoPauseOnFocus;
    [SerializeField, Range(0.04f, 0.3f)] private float selectionPulseScale = 0.12f;
    [SerializeField, Range(1f, 8f)] private float selectionPulseSpeed = 3.8f;
    [SerializeField, Range(0.05f, 1f)] private float interactionVolume = 0.22f;

    private const float EarthYearDays = 365.256f;
    private const float MoonOrbitDays = 27.3217f;
    private const float MoonDayHours = 655.72f;
    private const float MoonRadiusEarth = 0.2727f;
    private const float UiPadding = 16f;
    private const float ControlPanelWidth = 330f;
    private const float ControlPanelHeight = 198f;
    private const float InfoPanelWidth = 390f;
    private const float InfoPanelHeight = 250f;

    private static readonly PlanetDefinition[] PlanetDefinitions = PlanetCatalog.Definitions;

    private readonly List<OrbitBody> bodies = new List<OrbitBody>();
    private readonly List<AsteroidBody> asteroids = new List<AsteroidBody>();
    private readonly List<GameObject> orbitLineObjects = new List<GameObject>();
    private readonly List<SelectableBody> selectableBodies = new List<SelectableBody>();
    private readonly Dictionary<Collider, SelectableBody> selectableByCollider = new Dictionary<Collider, SelectableBody>();
    private readonly Dictionary<int, AudioClip> cachedSelectionClips = new Dictionary<int, AudioClip>();

    private Transform generatedRoot;
    private Camera sceneCamera;
    private SolarSystemCameraController cameraController;
    private AudioSource interactionAudioSource;
    private float simulatedDays;
    private Material orbitMaterial;
    private Material asteroidMaterial;
    private SelectableBody selectedBody;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapIfNeeded()
    {
        if (FindObjectOfType<SolarSystem>() != null)
        {
            return;
        }

        GameObject controller = new GameObject("Solar System");
        controller.AddComponent<SolarSystem>();
    }

    private void Start()
    {
        ConfigureRenderQuality();
        BuildSystem();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleSelectionInput();

        if (!paused)
        {
            simulatedDays += Time.deltaTime * daysPerSecond;
        }

        UpdatePlanets();
        UpdateAsteroids();
        UpdateSelectionEffects();
        FaceLabelsToCamera();
    }
}
