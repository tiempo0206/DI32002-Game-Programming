using UnityEngine;

internal sealed class OrbitBody
{
    public PlanetDefinition Definition;
    public Transform OrbitingRoot;
    public Transform AxisRoot;
    public Transform BodyTransform;
    public Renderer Renderer;
    public Material Material;
    public Transform Label;
    public Transform MoonOrbitingRoot;
    public Transform MoonAxisRoot;
    public Transform MoonBodyTransform;
    public Renderer MoonRenderer;
    public Material MoonMaterial;
    public Transform MoonLabel;
    public float VisualRadius;
}
