using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only setup utility for creating a ready-to-test MVP shooting scene.
/// Run from Tools/Splat Fighters/Create MVP Shooting Test Scene.
/// </summary>
public static class SplatFightersMvpSceneSetup
{
    private const string ScenePath = "Assets/Scenes/MVP_ShootingTest.unity";
    private const string ProjectilePrefabPath = "Assets/Prefabs/Weapons/InkProjectile.prefab";
    private const string MaterialsFolder = "Assets/Materials";
    private const string PrefabsFolder = "Assets/Prefabs";

    [MenuItem("Tools/Splat Fighters/Create MVP Shooting Test Scene")]
    public static void CreateMvpShootingTestScene()
    {
        EnsureFolders();

        Material groundMaterial = GetOrCreateMaterial("Assets/Materials/MAT_Ground_Debug.mat", new Color(0.35f, 0.35f, 0.35f));
        Material shooterMaterial = GetOrCreateMaterial("Assets/Materials/MAT_TestShooter_Debug.mat", new Color(0.1f, 0.35f, 0.9f));
        Material projectileMaterial = GetOrCreateMaterial("Assets/Materials/MAT_InkProjectile_TeamA.mat", new Color(0.05f, 0.45f, 1f));

        InkProjectile projectilePrefab = CreateOrUpdateProjectilePrefab(projectileMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MVP_ShootingTest";

        CreateLighting();
        CreateCamera();
        CreatePaintManager();
        CreatePaintableGround(groundMaterial);
        CreateTestShooter(shooterMaterial, projectilePrefab);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created MVP shooting test scene at {ScenePath}. Press Play and hold Mouse0 to shoot ink projectiles.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder(PrefabsFolder, "Weapons");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Scenes");
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static InkProjectile CreateOrUpdateProjectilePrefab(Material projectileMaterial)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "InkProjectile";
        projectileObject.transform.localScale = Vector3.one * 0.25f;

        MeshRenderer renderer = projectileObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = projectileMaterial;

        Rigidbody rb = projectileObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        InkProjectile projectile = projectileObject.AddComponent<InkProjectile>();
        SerializedObject projectileSo = new SerializedObject(projectile);
        projectileSo.FindProperty("defaultSpeed").floatValue = 18f;
        projectileSo.FindProperty("lifetime").floatValue = 4f;
        projectileSo.FindProperty("paintRadius").floatValue = 1.75f;
        projectileSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        projectileSo.FindProperty("destroyOnNonPaintableHit").boolValue = true;
        projectileSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, ProjectilePrefabPath);
        Object.DestroyImmediate(projectileObject);

        return prefab.GetComponent<InkProjectile>();
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 8f, -12f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.5f, 0f) - cameraObject.transform.position);
    }

    private static void CreatePaintManager()
    {
        GameObject managerObject = new GameObject("PaintManager");
        managerObject.AddComponent<PaintManager>();
    }

    private static void CreatePaintableGround(Material groundMaterial)
    {
        GameObject groundRoot = new GameObject("PaintableGround");
        groundRoot.transform.position = Vector3.zero;

        PaintableArea area = groundRoot.AddComponent<PaintableArea>();
        SerializedObject areaSo = new SerializedObject(area);
        areaSo.FindProperty("areaSize").vector2Value = new Vector2(20f, 20f);
        areaSo.FindProperty("gridWidth").intValue = 60;
        areaSo.FindProperty("gridHeight").intValue = 60;
        areaSo.FindProperty("resetOnAwake").boolValue = true;
        areaSo.FindProperty("drawGizmos").boolValue = true;
        areaSo.FindProperty("drawOnlyWhenSelected").boolValue = false;
        areaSo.FindProperty("drawPaintedCells").boolValue = true;
        areaSo.FindProperty("drawUnpaintedCells").boolValue = false;
        areaSo.FindProperty("drawGridLines").boolValue = false;
        areaSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "GroundVisual";
        visual.transform.SetParent(groundRoot.transform);
        visual.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        visual.transform.localScale = new Vector3(20f, 0.1f, 20f);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = groundMaterial;
    }

    private static void CreateTestShooter(Material shooterMaterial, InkProjectile projectilePrefab)
    {
        GameObject shooter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        shooter.name = "TestShooter";
        shooter.transform.position = new Vector3(0f, 1f, -8f);

        MeshRenderer renderer = shooter.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = shooterMaterial;

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(shooter.transform);
        firePoint.transform.position = new Vector3(0f, 1.35f, -7.35f);
        firePoint.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.05f, 0f) - firePoint.transform.position, Vector3.up);

        InkWeapon weapon = shooter.AddComponent<InkWeapon>();
        SerializedObject weaponSo = new SerializedObject(weapon);
        weaponSo.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        weaponSo.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        weaponSo.FindProperty("team").enumValueIndex = (int)Team.TeamA;
        weaponSo.FindProperty("projectileSpeed").floatValue = 18f;
        weaponSo.FindProperty("paintRadius").floatValue = 1.75f;
        weaponSo.FindProperty("fireCooldown").floatValue = 0.2f;
        weaponSo.FindProperty("useCameraAim").boolValue = false;
        weaponSo.FindProperty("enableKeyboardTestFire").boolValue = true;
        weaponSo.ApplyModifiedPropertiesWithoutUndo();
    }
}
