using UnityEngine;

internal enum PlanetStyle
{
    Rocky,
    Earth,
    GasGiant,
    IceGiant,
    Sun
}

internal struct PlanetDefinition
{
    public string Name;
    public float RadiusEarth;
    public float SemiMajorAxisAU;
    public float OrbitalPeriodDays;
    public float DayLengthHours;
    public float AxialTiltDegrees;
    public float InclinationDegrees;
    public float Eccentricity;
    public float InitialMeanAnomalyDegrees;
    public Color BaseColor;
    public PlanetStyle Style;
    public bool HasMoon;
    public bool HasRings;
    public Color RingColor;
    public float RingInnerRadiusMultiplier;
    public float RingOuterRadiusMultiplier;
    public string KidPrompt;
    public string KidFact;
    public Color HighlightColor;
    public float FocusDistanceMultiplier;
    public float TonePitch;
}
