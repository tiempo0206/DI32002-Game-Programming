using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight center-screen reticle for the MVP third-person shooter controls.
/// The reticle is built at runtime so the test scene does not need manual UI setup.
/// </summary>
public class AimReticleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform reticleRoot = null;
    [SerializeField] private Image centerDot = null;
    [SerializeField] private Image leftLine = null;
    [SerializeField] private Image rightLine = null;
    [SerializeField] private Image topLine = null;
    [SerializeField] private Image bottomLine = null;

    [Header("Colors")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color targetColor = new Color(0.05f, 0.85f, 1f, 1f);
    [SerializeField] private Color noTargetColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("Layout")]
    [SerializeField] private float baseGap = 15f;
    [SerializeField] private float targetGap = 10f;
    [SerializeField] private float noTargetGap = 22f;
    [SerializeField] private float lineLength = 16f;
    [SerializeField] private float lineThickness = 3f;
    [SerializeField] private float centerDotSize = 5f;
    [SerializeField] private float smoothSpeed = 18f;

    private float currentGap;

    public static AimReticleUI CreateRuntimeReticle()
    {
        GameObject canvasObject = new GameObject("AimReticleCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        AimReticleUI reticle = canvasObject.AddComponent<AimReticleUI>();
        reticle.BuildRuntimeElements(canvasObject.transform);
        return reticle;
    }

    private void Awake()
    {
        EnsureReferences();
        currentGap = baseGap;
        ApplyGap(currentGap);
        ApplyColor(idleColor);
    }

    public void SetState(bool hasAimTarget)
    {
        EnsureReferences();

        float desiredGap = hasAimTarget ? targetGap : noTargetGap;
        Color desiredColor = hasAimTarget ? targetColor : noTargetColor;

        currentGap = Mathf.Lerp(currentGap, desiredGap, GetSharpness(smoothSpeed));
        ApplyGap(currentGap);
        ApplyColor(desiredColor);
    }

    private void BuildRuntimeElements(Transform canvasTransform)
    {
        GameObject rootObject = new GameObject("AimReticle");
        rootObject.transform.SetParent(canvasTransform, false);

        reticleRoot = rootObject.AddComponent<RectTransform>();
        reticleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        reticleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        reticleRoot.pivot = new Vector2(0.5f, 0.5f);
        reticleRoot.anchoredPosition = Vector2.zero;
        reticleRoot.sizeDelta = new Vector2(120f, 120f);

        centerDot = CreateSegment(reticleRoot, "CenterDot", new Vector2(centerDotSize, centerDotSize));
        leftLine = CreateSegment(reticleRoot, "LeftLine", new Vector2(lineLength, lineThickness));
        rightLine = CreateSegment(reticleRoot, "RightLine", new Vector2(lineLength, lineThickness));
        topLine = CreateSegment(reticleRoot, "TopLine", new Vector2(lineThickness, lineLength));
        bottomLine = CreateSegment(reticleRoot, "BottomLine", new Vector2(lineThickness, lineLength));
    }

    private void EnsureReferences()
    {
        if (reticleRoot == null)
        {
            reticleRoot = GetComponentInChildren<RectTransform>();
        }
    }

    private static Image CreateSegment(Transform parent, string name, Vector2 size)
    {
        GameObject segmentObject = new GameObject(name);
        segmentObject.transform.SetParent(parent, false);

        RectTransform rect = segmentObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = segmentObject.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void ApplyGap(float gap)
    {
        SetAnchoredPosition(leftLine, new Vector2(-gap - lineLength * 0.5f, 0f));
        SetAnchoredPosition(rightLine, new Vector2(gap + lineLength * 0.5f, 0f));
        SetAnchoredPosition(topLine, new Vector2(0f, gap + lineLength * 0.5f));
        SetAnchoredPosition(bottomLine, new Vector2(0f, -gap - lineLength * 0.5f));
        SetAnchoredPosition(centerDot, Vector2.zero);
    }

    private void ApplyColor(Color color)
    {
        SetColor(centerDot, color);
        SetColor(leftLine, color);
        SetColor(rightLine, color);
        SetColor(topLine, color);
        SetColor(bottomLine, color);
    }

    private static void SetAnchoredPosition(Image image, Vector2 anchoredPosition)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void SetColor(Image image, Color color)
    {
        if (image != null)
        {
            image.color = color;
        }
    }

    private static float GetSharpness(float speed)
    {
        if (speed <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-speed * Time.deltaTime);
    }
}
