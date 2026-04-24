using UnityEngine;

internal static class PlanetCatalog
{
    public static readonly PlanetDefinition[] Definitions =
    {
        new PlanetDefinition
        {
            Name = "Mercury",
            RadiusEarth = 0.383f,
            SemiMajorAxisAU = 0.387f,
            OrbitalPeriodDays = 87.969f,
            DayLengthHours = 1407.6f,
            AxialTiltDegrees = 0.03f,
            InclinationDegrees = 7.00f,
            Eccentricity = 0.2056f,
            InitialMeanAnomalyDegrees = 174.8f,
            BaseColor = new Color(0.55f, 0.51f, 0.46f),
            Style = PlanetStyle.Rocky,
            KidPrompt = "Mercury is the speedy little planet.",
            KidFact = "It races around the Sun in only 88 Earth days.",
            HighlightColor = new Color(0.93f, 0.77f, 0.56f),
            FocusDistanceMultiplier = 4.4f,
            TonePitch = 1.30f
        },
        new PlanetDefinition
        {
            Name = "Venus",
            RadiusEarth = 0.949f,
            SemiMajorAxisAU = 0.723f,
            OrbitalPeriodDays = 224.701f,
            DayLengthHours = -5832.5f,
            AxialTiltDegrees = 177.36f,
            InclinationDegrees = 3.39f,
            Eccentricity = 0.0068f,
            InitialMeanAnomalyDegrees = 50.4f,
            BaseColor = new Color(0.93f, 0.74f, 0.46f),
            Style = PlanetStyle.Rocky,
            KidPrompt = "Venus hides under bright, thick clouds.",
            KidFact = "It spins the opposite way from many planets and gets very hot.",
            HighlightColor = new Color(1f, 0.84f, 0.48f),
            FocusDistanceMultiplier = 4.7f,
            TonePitch = 1.08f
        },
        new PlanetDefinition
        {
            Name = "Earth",
            RadiusEarth = 1.0f,
            SemiMajorAxisAU = 1.0f,
            OrbitalPeriodDays = 365.256f,
            DayLengthHours = 23.934f,
            AxialTiltDegrees = 23.44f,
            InclinationDegrees = 0f,
            Eccentricity = 0.0167f,
            InitialMeanAnomalyDegrees = 357.5f,
            BaseColor = new Color(0.18f, 0.43f, 0.85f),
            Style = PlanetStyle.Earth,
            HasMoon = true,
            KidPrompt = "Earth is our home planet.",
            KidFact = "It has oceans, weather, and one moon that circles around it.",
            HighlightColor = new Color(0.42f, 0.82f, 1f),
            FocusDistanceMultiplier = 4.8f,
            TonePitch = 1.00f
        },
        new PlanetDefinition
        {
            Name = "Mars",
            RadiusEarth = 0.532f,
            SemiMajorAxisAU = 1.524f,
            OrbitalPeriodDays = 686.98f,
            DayLengthHours = 24.623f,
            AxialTiltDegrees = 25.19f,
            InclinationDegrees = 1.85f,
            Eccentricity = 0.0934f,
            InitialMeanAnomalyDegrees = 19.4f,
            BaseColor = new Color(0.76f, 0.31f, 0.18f),
            Style = PlanetStyle.Rocky,
            KidPrompt = "Mars is the dusty red planet.",
            KidFact = "It has giant volcanoes and huge dust storms.",
            HighlightColor = new Color(1f, 0.5f, 0.32f),
            FocusDistanceMultiplier = 4.5f,
            TonePitch = 1.18f
        },
        new PlanetDefinition
        {
            Name = "Jupiter",
            RadiusEarth = 11.21f,
            SemiMajorAxisAU = 5.203f,
            OrbitalPeriodDays = 4332.59f,
            DayLengthHours = 9.925f,
            AxialTiltDegrees = 3.13f,
            InclinationDegrees = 1.30f,
            Eccentricity = 0.0489f,
            InitialMeanAnomalyDegrees = 20.0f,
            BaseColor = new Color(0.78f, 0.61f, 0.43f),
            Style = PlanetStyle.GasGiant,
            KidPrompt = "Jupiter is the giant of the solar system.",
            KidFact = "Its Great Red Spot is a giant storm bigger than Earth.",
            HighlightColor = new Color(1f, 0.78f, 0.55f),
            FocusDistanceMultiplier = 2.4f,
            TonePitch = 0.84f
        },
        new PlanetDefinition
        {
            Name = "Saturn",
            RadiusEarth = 9.45f,
            SemiMajorAxisAU = 9.537f,
            OrbitalPeriodDays = 10759.22f,
            DayLengthHours = 10.656f,
            AxialTiltDegrees = 26.73f,
            InclinationDegrees = 2.49f,
            Eccentricity = 0.0565f,
            InitialMeanAnomalyDegrees = 317.0f,
            BaseColor = new Color(0.86f, 0.75f, 0.52f),
            Style = PlanetStyle.GasGiant,
            HasRings = true,
            RingColor = new Color(0.86f, 0.78f, 0.62f, 0.62f),
            RingInnerRadiusMultiplier = 1.25f,
            RingOuterRadiusMultiplier = 2.35f,
            KidPrompt = "Saturn shines with amazing rings.",
            KidFact = "Its rings are made from icy chunks and rocky pieces.",
            HighlightColor = new Color(1f, 0.89f, 0.58f),
            FocusDistanceMultiplier = 2.7f,
            TonePitch = 0.90f
        },
        new PlanetDefinition
        {
            Name = "Uranus",
            RadiusEarth = 4.01f,
            SemiMajorAxisAU = 19.191f,
            OrbitalPeriodDays = 30688.5f,
            DayLengthHours = -17.24f,
            AxialTiltDegrees = 97.77f,
            InclinationDegrees = 0.77f,
            Eccentricity = 0.0472f,
            InitialMeanAnomalyDegrees = 142.2f,
            BaseColor = new Color(0.46f, 0.82f, 0.88f),
            Style = PlanetStyle.IceGiant,
            HasRings = true,
            RingColor = new Color(0.55f, 0.72f, 0.78f, 0.42f),
            RingInnerRadiusMultiplier = 1.35f,
            RingOuterRadiusMultiplier = 1.75f,
            KidPrompt = "Uranus rolls through space on its side.",
            KidFact = "It looks blue-green because methane gas changes the color of the light.",
            HighlightColor = new Color(0.61f, 0.95f, 1f),
            FocusDistanceMultiplier = 3.2f,
            TonePitch = 0.95f
        },
        new PlanetDefinition
        {
            Name = "Neptune",
            RadiusEarth = 3.88f,
            SemiMajorAxisAU = 30.07f,
            OrbitalPeriodDays = 60182.0f,
            DayLengthHours = 16.11f,
            AxialTiltDegrees = 28.32f,
            InclinationDegrees = 1.77f,
            Eccentricity = 0.0086f,
            InitialMeanAnomalyDegrees = 256.2f,
            BaseColor = new Color(0.18f, 0.32f, 0.82f),
            Style = PlanetStyle.IceGiant,
            KidPrompt = "Neptune is dark blue and very stormy.",
            KidFact = "Some of the fastest winds in the solar system blow there.",
            HighlightColor = new Color(0.42f, 0.59f, 1f),
            FocusDistanceMultiplier = 3.2f,
            TonePitch = 0.88f
        }
    };
}
