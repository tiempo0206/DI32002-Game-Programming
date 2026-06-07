using UnityEngine;

/// <summary>
/// Displays a rotating, animated, character-tinted prefab on the menu selection screen.
/// </summary>
[DisallowMultipleComponent]
public sealed class CharacterPreviewPresenter : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int Color01Id = Shader.PropertyToID("_Color01");
    private static readonly int Color02Id = Shader.PropertyToID("_Color02");
    private static readonly int Color03Id = Shader.PropertyToID("_Color03");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField, Min(0f)] private float rotationSpeed = 22f;
    [SerializeField, Min(0.5f)] private float targetHeight = 3.1f;
    [SerializeField, Min(0.5f)] private float maxFootprint = 2.2f;

    private CharacterVisualCatalog catalog;
    private CharacterVisualOption currentOption;
    private Team team;
    private int selectedIndex;
    private GameObject currentInstance;
    private MaterialPropertyBlock propertyBlock;

    public int SelectedIndex => selectedIndex;
    public string CurrentDisplayName => currentOption != null ? currentOption.DisplayName : "Unavailable";

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    public void Configure(CharacterVisualCatalog newCatalog, Team newTeam, int initialIndex)
    {
        catalog = newCatalog;
        team = newTeam;
        Select(initialIndex);
    }

    public void ConfigureStageFit(float newTargetHeight, float newMaxFootprint)
    {
        targetHeight = Mathf.Max(0.5f, newTargetHeight);
        maxFootprint = Mathf.Max(0.5f, newMaxFootprint);

        if (currentInstance != null)
        {
            FitPreviewToStage();
        }
    }

    public void Select(int index)
    {
        if (catalog == null || catalog.Count == 0)
        {
            return;
        }

        selectedIndex = catalog.NormalizeIndex(index);
        currentOption = catalog.GetOption(selectedIndex);

        if (currentOption == null || currentOption.Prefab == null)
        {
            return;
        }

        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        currentInstance = Instantiate(currentOption.Prefab, transform);
        currentInstance.name = $"Preview_{team}_{currentOption.DisplayName}";
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.Euler(currentOption.LocalEulerOffset);
        currentInstance.transform.localScale = Vector3.one;

        FitPreviewToStage();
        ApplyCharacterTint();
        PlayIdleAnimation();
    }

    private void FitPreviewToStage()
    {
        Renderer[] renderers = currentInstance.GetComponentsInChildren<Renderer>(true);
        if (!TryCalculateBounds(renderers, out Bounds bounds))
        {
            return;
        }

        Vector3 size = bounds.size;
        float scaleByHeight = size.y > 0.001f ? targetHeight / size.y : 1f;
        float widest = Mathf.Max(size.x, size.z);
        float scaleByFootprint = widest > 0.001f ? maxFootprint / widest : scaleByHeight;
        float uniformScale = Mathf.Min(scaleByHeight, scaleByFootprint);

        currentInstance.transform.localScale = Vector3.one * uniformScale;
        currentInstance.transform.localPosition = new Vector3(
            -bounds.center.x * uniformScale,
            -bounds.min.y * uniformScale,
            -bounds.center.z * uniformScale);
    }

    private bool TryCalculateBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Vector3 min = currentInstance.transform.InverseTransformPoint(renderer.bounds.min);
            Vector3 max = currentInstance.transform.InverseTransformPoint(renderer.bounds.max);
            Bounds localBounds = new Bounds((min + max) * 0.5f, Vector3.zero);
            localBounds.Encapsulate(min);
            localBounds.Encapsulate(max);

            if (!hasBounds)
            {
                bounds = localBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localBounds);
            }
        }

        return hasBounds;
    }

    private void ApplyCharacterTint()
    {
        Renderer[] renderers = currentInstance.GetComponentsInChildren<Renderer>(true);
        Color primary = currentOption.InkColor;
        Color bright = Color.Lerp(primary, Color.white, 0.24f);
        Color dark = Color.Lerp(primary, Color.black, 0.35f);

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, primary);
            propertyBlock.SetColor(ColorId, primary);
            propertyBlock.SetColor(Color01Id, primary);
            propertyBlock.SetColor(Color02Id, bright);
            propertyBlock.SetColor(Color03Id, dark);
            propertyBlock.SetColor(EmissionColorId, primary * 0.35f);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void PlayIdleAnimation()
    {
        Animator[] animators = currentInstance.GetComponentsInChildren<Animator>(true);
        int stateHash = Animator.StringToHash(currentOption.IdleState);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, 0.08f, 0, 0f);
            }
        }
    }
}
