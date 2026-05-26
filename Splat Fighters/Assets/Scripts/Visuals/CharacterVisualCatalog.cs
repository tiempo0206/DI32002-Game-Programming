using System;
using UnityEngine;

/// <summary>
/// Runtime-loadable list of selectable character prefabs and their animation state names.
/// </summary>
[CreateAssetMenu(fileName = "CharacterVisualCatalog", menuName = "Splat Fighters/Character Visual Catalog")]
public sealed class CharacterVisualCatalog : ScriptableObject
{
    [SerializeField] private CharacterVisualOption[] options = Array.Empty<CharacterVisualOption>();

    public int Count => options != null ? options.Length : 0;

    public static CharacterVisualCatalog LoadDefault()
    {
        CharacterVisualCatalog catalog = Resources.Load<CharacterVisualCatalog>("CharacterVisualCatalog");
        return catalog != null ? catalog : CreateRuntimeFallback();
    }

    public CharacterVisualOption GetOption(int index)
    {
        if (Count == 0)
        {
            return null;
        }

        return options[NormalizeIndex(index)];
    }

    public int NormalizeIndex(int index)
    {
        if (Count == 0)
        {
            return 0;
        }

        int normalized = index % Count;
        return normalized < 0 ? normalized + Count : normalized;
    }

#if UNITY_EDITOR
    public void ReplaceOptions(CharacterVisualOption[] newOptions)
    {
        options = newOptions ?? Array.Empty<CharacterVisualOption>();
    }
#endif

    private static CharacterVisualCatalog CreateRuntimeFallback()
    {
        CharacterVisualOption[] fallbackOptions =
        {
            CreateResourceOption("Bat", "BatPBRMaskTint", "IdleNormal", "FlyFWD", "SenseSomethingST", "Attack01", 1.25f, 1.25f, 0.28f, 1.35f, 1.5f),
            CreateResourceOption("Dragon", "DragonPBRMaskTint", "IdleNormal", "FlyFWD", "SenseSomethingST", "Attack01", 1.45f, 1.25f, 0.3f, 1.35f, 1.5f),
            CreateResourceOption("Evil Mage", "EvilMagePBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.05f, 0.34f, 1.2f, 1.35f),
            CreateResourceOption("Golem", "GolemPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.48f, 1.2f, 0.34f, 1.22f, 1.4f),
            CreateResourceOption("Monster Plant", "MonsterPlantPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.38f, 1.15f, 0.32f, 1.3f, 1.45f),
            CreateResourceOption("Orc", "OrcPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.1f, 0.34f, 1.22f, 1.4f),
            CreateResourceOption("Skeleton", "SkeletonPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.05f, 0.34f, 1.22f, 1.38f),
            CreateResourceOption("Slime", "SlimePBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.05f, 1.05f, 0.28f, 1.35f, 1.5f),
            CreateResourceOption("Spider", "SpiderPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.1f, 1.25f, 0.28f, 1.4f, 1.55f),
            CreateResourceOption("Turtle Shell", "TurtleShellPBRMaskTint", "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.25f, 1.15f, 0.3f, 1.35f, 1.45f)
        };

        CharacterVisualCatalog catalog = CreateInstance<CharacterVisualCatalog>();
        catalog.options = Array.FindAll(fallbackOptions, option => option != null && option.Prefab != null);
        return catalog.Count > 0 ? catalog : null;
    }

    private static CharacterVisualOption CreateResourceOption(
        string displayName,
        string prefabName,
        string idleState,
        string moveState,
        string swimState,
        string attackState,
        float targetHeight,
        float maxFootprint,
        float swimHeightMultiplier,
        float swimWidthMultiplier,
        float swimLengthMultiplier)
    {
        GameObject prefab = Resources.Load<GameObject>($"CharacterPrefabs/{prefabName}");

        if (prefab == null)
        {
            return null;
        }

        return new CharacterVisualOption(
            displayName,
            prefab,
            idleState,
            moveState,
            swimState,
            attackState,
            targetHeight,
            maxFootprint,
            swimHeightMultiplier,
            swimWidthMultiplier,
            swimLengthMultiplier);
    }
}

[Serializable]
public sealed class CharacterVisualOption
{
    [SerializeField] private string displayName = "Character";
    [SerializeField] private GameObject prefab = null;
    [SerializeField] private string idleState = "IdleNormal";
    [SerializeField] private string moveState = "RunFWD";
    [SerializeField] private string swimState = "SenseSomethingST";
    [SerializeField] private string attackState = "Attack01";
    [SerializeField, Min(0.2f)] private float targetHeight = 1.45f;
    [SerializeField, Min(0.2f)] private float maxFootprint = 1.1f;
    [SerializeField, Range(0.15f, 1f)] private float swimHeightMultiplier = 0.34f;
    [SerializeField, Min(0.5f)] private float swimWidthMultiplier = 1.25f;
    [SerializeField, Min(0.5f)] private float swimLengthMultiplier = 1.4f;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;

    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public string IdleState => idleState;
    public string MoveState => moveState;
    public string SwimState => swimState;
    public string AttackState => attackState;
    public float TargetHeight => targetHeight;
    public float MaxFootprint => maxFootprint;
    public float SwimHeightMultiplier => swimHeightMultiplier;
    public float SwimWidthMultiplier => swimWidthMultiplier;
    public float SwimLengthMultiplier => swimLengthMultiplier;
    public Vector3 LocalEulerOffset => localEulerOffset;

    public CharacterVisualOption(
        string displayName,
        GameObject prefab,
        string idleState,
        string moveState,
        string swimState,
        string attackState,
        float targetHeight = 1.45f,
        float maxFootprint = 1.1f,
        float swimHeightMultiplier = 0.34f,
        float swimWidthMultiplier = 1.25f,
        float swimLengthMultiplier = 1.4f,
        Vector3 localEulerOffset = default)
    {
        this.displayName = displayName;
        this.prefab = prefab;
        this.idleState = idleState;
        this.moveState = moveState;
        this.swimState = swimState;
        this.attackState = attackState;
        this.targetHeight = targetHeight;
        this.maxFootprint = maxFootprint;
        this.swimHeightMultiplier = swimHeightMultiplier;
        this.swimWidthMultiplier = swimWidthMultiplier;
        this.swimLengthMultiplier = swimLengthMultiplier;
        this.localEulerOffset = localEulerOffset;
    }
}
