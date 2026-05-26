using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Replaces the prototype capsule renderer with an imported character prefab while keeping gameplay colliders intact.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
public sealed class CharacterVisualController : MonoBehaviour
{
    private const float CharacterHalfHeight = 0.78f;
    private const float MinStateRefreshInterval = 0.12f;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int Color01Id = Shader.PropertyToID("_Color01");
    private static readonly int Color02Id = Shader.PropertyToID("_Color02");
    private static readonly int Color03Id = Shader.PropertyToID("_Color03");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly List<Renderer> legacyRenderers = new List<Renderer>();
    private readonly List<GameObject> legacySwimObjects = new List<GameObject>();

    private CharacterVisualCatalog catalog;
    private CharacterVisualOption currentOption;
    private Team team = Team.None;
    private int selectedIndex;
    private bool isSwimming;
    private Transform visualPivot;
    private GameObject currentInstance;
    private Renderer[] activeRenderers = new Renderer[0];
    private Animator[] animators = new Animator[0];
    private MaterialPropertyBlock propertyBlock;
    private Vector3 previousPosition;
    private Vector3 basePivotPosition;
    private Vector3 basePivotScale = Vector3.one;
    private string currentAnimationState;
    private float attackUntilTime;
    private float nextStateRefreshTime;
    private InkWeapon weapon;

    public bool HasActiveVisual => currentInstance != null;
    public int SelectedIndex => selectedIndex;
    public string CurrentDisplayName => currentOption != null ? currentOption.DisplayName : string.Empty;

    private void OnEnable()
    {
        weapon = GetComponentInChildren<InkWeapon>();

        if (weapon != null)
        {
            weapon.Fired += HandleWeaponFired;
        }
    }

    private void OnDisable()
    {
        if (weapon != null)
        {
            weapon.Fired -= HandleWeaponFired;
        }
    }

    private void Update()
    {
        ApplyLegacyVisibility();
        UpdateAnimation();
        previousPosition = transform.position;
    }

    public void Configure(CharacterVisualCatalog newCatalog, Team newTeam, int initialIndex)
    {
        catalog = newCatalog;
        team = newTeam;
        HidePrototypeRenderers();
        Select(initialIndex);
    }

    public void Select(int newIndex)
    {
        if (catalog == null || catalog.Count == 0)
        {
            return;
        }

        selectedIndex = catalog.NormalizeIndex(newIndex);
        currentOption = catalog.GetOption(selectedIndex);

        if (currentOption == null || currentOption.Prefab == null)
        {
            return;
        }

        if (currentInstance != null)
        {
            Destroy(currentInstance);
        }

        EnsureVisualPivot();
        currentInstance = Instantiate(currentOption.Prefab, visualPivot);
        currentInstance.name = $"CharacterVisual_{currentOption.DisplayName}";
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.Euler(currentOption.LocalEulerOffset);
        currentInstance.transform.localScale = Vector3.one;

        activeRenderers = currentInstance.GetComponentsInChildren<Renderer>(true);
        animators = currentInstance.GetComponentsInChildren<Animator>(true);
        FitToCharacterController();
        ApplyTeamTint();
        ApplySwimState();

        currentAnimationState = string.Empty;
        previousPosition = transform.position;
        CrossFadeTo(currentOption.IdleState, 0.05f, true);
    }

    public void SetSwimming(bool swimming)
    {
        if (isSwimming == swimming)
        {
            return;
        }

        isSwimming = swimming;
        ApplySwimState();
    }

    public void SetTeam(Team newTeam)
    {
        if (team == newTeam)
        {
            return;
        }

        team = newTeam;
        ApplyTeamTint();
    }

    private void HidePrototypeRenderers()
    {
        legacyRenderers.Clear();
        legacySwimObjects.Clear();

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            legacyRenderers.Add(rootRenderer);
        }

        Transform swimForm = transform.Find("SwimFormVisual");
        if (swimForm != null)
        {
            legacySwimObjects.Add(swimForm.gameObject);
            Renderer[] swimRenderers = swimForm.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < swimRenderers.Length; i++)
            {
                if (swimRenderers[i] != null && !legacyRenderers.Contains(swimRenderers[i]))
                {
                    legacyRenderers.Add(swimRenderers[i]);
                }
            }
        }

        ApplyLegacyVisibility();
    }

    private void ApplyLegacyVisibility()
    {
        for (int i = 0; i < legacyRenderers.Count; i++)
        {
            if (legacyRenderers[i] != null)
            {
                legacyRenderers[i].enabled = false;
            }
        }

        for (int i = 0; i < legacySwimObjects.Count; i++)
        {
            if (legacySwimObjects[i] != null && legacySwimObjects[i].activeSelf)
            {
                legacySwimObjects[i].SetActive(false);
            }
        }
    }

    private void EnsureVisualPivot()
    {
        if (visualPivot != null)
        {
            return;
        }

        GameObject pivotObject = new GameObject("CharacterVisualPivot");
        pivotObject.transform.SetParent(transform, false);
        visualPivot = pivotObject.transform;
        basePivotPosition = Vector3.zero;
        basePivotScale = Vector3.one;
    }

    private void FitToCharacterController()
    {
        Bounds localBounds;
        if (!TryCalculateLocalBounds(out localBounds))
        {
            visualPivot.localPosition = Vector3.zero;
            visualPivot.localScale = Vector3.one;
            basePivotPosition = visualPivot.localPosition;
            basePivotScale = visualPivot.localScale;
            return;
        }

        Vector3 size = localBounds.size;
        float scaleByHeight = size.y > 0.001f ? currentOption.TargetHeight / size.y : 1f;
        float widest = Mathf.Max(size.x, size.z);
        float scaleByFootprint = widest > 0.001f ? currentOption.MaxFootprint / widest : scaleByHeight;
        float uniformScale = Mathf.Min(scaleByHeight, scaleByFootprint);

        Vector3 center = localBounds.center;
        float bottom = localBounds.min.y;
        basePivotScale = Vector3.one * uniformScale;
        basePivotPosition = new Vector3(
            -center.x * uniformScale,
            -CharacterHalfHeight - bottom * uniformScale,
            -center.z * uniformScale);

        visualPivot.localPosition = basePivotPosition;
        visualPivot.localScale = basePivotScale;
    }

    private bool TryCalculateLocalBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < activeRenderers.Length; i++)
        {
            Renderer renderer = activeRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            Vector3 min = transform.InverseTransformPoint(rendererBounds.min);
            Vector3 max = transform.InverseTransformPoint(rendererBounds.max);
            Bounds localRendererBounds = new Bounds((min + max) * 0.5f, Vector3.zero);
            localRendererBounds.Encapsulate(min);
            localRendererBounds.Encapsulate(max);

            if (!hasBounds)
            {
                bounds = localRendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localRendererBounds);
            }
        }

        return hasBounds;
    }

    private void ApplySwimState()
    {
        if (visualPivot == null || currentOption == null)
        {
            return;
        }

        if (!isSwimming)
        {
            visualPivot.localPosition = basePivotPosition;
            visualPivot.localScale = basePivotScale;
            ApplyTeamTint();
            return;
        }

        float swimHeight = currentOption.SwimHeightMultiplier;
        visualPivot.localPosition = basePivotPosition + Vector3.down * CharacterHalfHeight * (1f - swimHeight);
        visualPivot.localScale = new Vector3(
            basePivotScale.x * currentOption.SwimWidthMultiplier,
            basePivotScale.y * swimHeight,
            basePivotScale.z * currentOption.SwimLengthMultiplier);
        ApplyTeamTint();
    }

    private void ApplyTeamTint()
    {
        if (activeRenderers == null || activeRenderers.Length == 0)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Color primary = TeamVisualPalette.GetColor(team);
        Color bright = Color.Lerp(primary, Color.white, 0.24f);
        Color dark = Color.Lerp(primary, Color.black, 0.35f);

        if (isSwimming)
        {
            primary = Color.Lerp(primary, Color.white, 0.12f);
            bright = Color.Lerp(bright, Color.white, 0.18f);
        }

        for (int i = 0; i < activeRenderers.Length; i++)
        {
            Renderer targetRenderer = activeRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, primary);
            propertyBlock.SetColor(ColorId, primary);
            propertyBlock.SetColor(Color01Id, primary);
            propertyBlock.SetColor(Color02Id, bright);
            propertyBlock.SetColor(Color03Id, dark);
            propertyBlock.SetColor(EmissionColorId, primary * 0.35f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void UpdateAnimation()
    {
        if (currentOption == null || animators == null || animators.Length == 0 || Time.time < nextStateRefreshTime)
        {
            return;
        }

        nextStateRefreshTime = Time.time + MinStateRefreshInterval;

        if (Time.time < attackUntilTime)
        {
            CrossFadeTo(currentOption.AttackState, 0.05f, false);
            return;
        }

        if (isSwimming)
        {
            CrossFadeTo(currentOption.SwimState, 0.12f, false);
            return;
        }

        Vector3 planarDelta = transform.position - previousPosition;
        planarDelta.y = 0f;
        bool isMoving = planarDelta.sqrMagnitude > 0.00008f;
        CrossFadeTo(isMoving ? currentOption.MoveState : currentOption.IdleState, 0.12f, false);
    }

    private void HandleWeaponFired()
    {
        attackUntilTime = Time.time + 0.38f;
        CrossFadeTo(currentOption != null ? currentOption.AttackState : string.Empty, 0.04f, true);
    }

    private void CrossFadeTo(string stateName, float fadeDuration, bool forceRestart)
    {
        if (string.IsNullOrWhiteSpace(stateName) || !forceRestart && stateName == currentAnimationState)
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        bool playedAny = false;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null || animator.runtimeAnimatorController == null || !animator.HasState(0, stateHash))
            {
                continue;
            }

            animator.CrossFadeInFixedTime(stateHash, fadeDuration, 0, 0f);
            playedAny = true;
        }

        if (playedAny)
        {
            currentAnimationState = stateName;
        }
    }
}
