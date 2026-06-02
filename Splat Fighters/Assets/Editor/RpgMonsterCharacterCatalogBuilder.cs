using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a Resources catalog so runtime code can load Asset Store prefabs without hard scene references.
/// </summary>
[InitializeOnLoad]
public static class RpgMonsterCharacterCatalogBuilder
{
    private const string CatalogPath = "Assets/Resources/CharacterVisualCatalog.asset";
    private const string PrefabRoot = "Assets/RPG Monster Wave PBR/Prefabs/PBRMaskTint";

    static RpgMonsterCharacterCatalogBuilder()
    {
        EditorApplication.delayCall += EnsureCatalog;
    }

    [MenuItem("Splat Fighters/Characters/Rebuild RPG Monster Catalog")]
    public static void EnsureCatalog()
    {
        if (!AssetDatabase.IsValidFolder(PrefabRoot))
        {
            return;
        }

        CharacterVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<CharacterVisualCatalog>(CatalogPath);

        if (catalog == null)
        {
            EnsureResourcesFolder();
            catalog = ScriptableObject.CreateInstance<CharacterVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        List<CharacterVisualOption> options = new List<CharacterVisualOption>
        {
            CreateOption("Bat", "BatPBRMaskTint", CharacterInkPalette.Bat, "IdleNormal", "FlyFWD", "SenseSomethingST", "Attack01", 1.25f, 1.25f, 0.28f, 1.35f, 1.5f),
            CreateOption("Dragon", "DragonPBRMaskTint", CharacterInkPalette.Dragon, "IdleNormal", "FlyFWD", "SenseSomethingST", "Attack01", 1.45f, 1.25f, 0.3f, 1.35f, 1.5f),
            CreateOption("Evil Mage", "EvilMagePBRMaskTint", CharacterInkPalette.EvilMage, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.05f, 0.34f, 1.2f, 1.35f),
            CreateOption("Golem", "GolemPBRMaskTint", CharacterInkPalette.Golem, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.48f, 1.2f, 0.34f, 1.22f, 1.4f),
            CreateOption("Monster Plant", "MonsterPlantPBRMaskTint", CharacterInkPalette.MonsterPlant, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.38f, 1.15f, 0.32f, 1.3f, 1.45f),
            CreateOption("Orc", "OrcPBRMaskTint", CharacterInkPalette.Orc, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.1f, 0.34f, 1.22f, 1.4f),
            CreateOption("Skeleton", "SkeletonPBRMaskTint", CharacterInkPalette.Skeleton, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.45f, 1.05f, 0.34f, 1.22f, 1.38f),
            CreateOption("Slime", "SlimePBRMaskTint", CharacterInkPalette.Slime, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.05f, 1.05f, 0.28f, 1.35f, 1.5f),
            CreateOption("Spider", "SpiderPBRMaskTint", CharacterInkPalette.Spider, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.1f, 1.25f, 0.28f, 1.4f, 1.55f),
            CreateOption("Turtle Shell", "TurtleShellPBRMaskTint", CharacterInkPalette.TurtleShell, "IdleNormal", "RunFWD", "SenseSomethingST", "Attack01", 1.25f, 1.15f, 0.3f, 1.35f, 1.45f)
        };

        options.RemoveAll(option => option == null || option.Prefab == null);

        if (options.Count == 0)
        {
            return;
        }

        catalog.ReplaceOptions(options.ToArray());
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static CharacterVisualOption CreateOption(
        string displayName,
        string prefabName,
        Color inkColor,
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
        string prefabPath = $"{PrefabRoot}/{prefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            return null;
        }

        return new CharacterVisualOption(
            displayName,
            prefab,
            inkColor,
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

    private static void EnsureResourcesFolder()
    {
        const string resourcesPath = "Assets/Resources";

        if (AssetDatabase.IsValidFolder(resourcesPath))
        {
            return;
        }

        Directory.CreateDirectory(resourcesPath);
        AssetDatabase.ImportAsset(resourcesPath);
    }
}
