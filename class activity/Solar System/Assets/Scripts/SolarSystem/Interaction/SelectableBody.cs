using UnityEngine;

internal sealed class SelectableBody
{
    public string Name;
    public string Prompt;
    public string Fact;
    public Transform FocusTransform;
    public Transform VisualTransform;
    public Renderer Renderer;
    public Material Material;
    public Collider Collider;
    public Vector3 BaseScale;
    public Color BaseEmission;
    public Color HighlightColor;
    public float FocusDistance;
    public float TonePitch;
}
