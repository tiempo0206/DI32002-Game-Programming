using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility that wires project-original audio assets into reusable prefabs.
/// </summary>
public static class SplatFightersAudioSetup
{
    private const string AudioRootFolder = "Assets/Resources/Audio";
    private const string MusicFolder = AudioRootFolder + "/Music";
    private const string SfxFolder = AudioRootFolder + "/SFX";
    private const string AudioPrefabFolder = AudioRootFolder + "/Prefabs";
    private const string AudioManagerPrefabPath = AudioPrefabFolder + "/SplatAudioManager.prefab";
    private const string MainMenuCanvasPrefabPath = "Assets/Resources/UI/MainMenu/Prefabs/MainMenuCanvas.prefab";

    [MenuItem("Tools/Splat Fighters/Apply Audio Presentation Setup")]
    public static void ApplyAudioPresentationSetup()
    {
        EnsureFolders();
        AssetDatabase.ImportAsset(AudioRootFolder, ImportAssetOptions.ImportRecursive);
        CreateOrUpdateAudioManagerPrefab();
        UpdateMainMenuAudioControls();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Splat Fighters audio presentation setup.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Audio");
        EnsureFolder(AudioRootFolder, "Music");
        EnsureFolder(AudioRootFolder, "SFX");
        EnsureFolder(AudioRootFolder, "Prefabs");
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string path = $"{parent}/{folderName}";

        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void CreateOrUpdateAudioManagerPrefab()
    {
        GameObject root = File.Exists(AudioManagerPrefabPath)
            ? PrefabUtility.LoadPrefabContents(AudioManagerPrefabPath)
            : new GameObject("SplatAudioManager");
        bool loadedPrefabContents = File.Exists(AudioManagerPrefabPath);

        SplatAudioManager manager = GetOrAddComponent<SplatAudioManager>(root);
        AudioSource musicSource = GetOrCreateAudioSource(root.transform, "MusicSource", true);
        AudioSource sfxSource = GetOrCreateAudioSource(root.transform, "SfxSource", false);

        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("musicSource").objectReferenceValue = musicSource;
        managerSo.FindProperty("sfxSource").objectReferenceValue = sfxSource;
        managerSo.FindProperty("menuMusic").objectReferenceValue = LoadClip(MusicFolder + "/MenuLoop.wav");
        managerSo.FindProperty("gameplayMusic").objectReferenceValue = LoadClip(MusicFolder + "/GameplayLoop.wav");
        managerSo.FindProperty("uiClickClip").objectReferenceValue = LoadClip(SfxFolder + "/UiClick.wav");
        managerSo.FindProperty("uiConfirmClip").objectReferenceValue = LoadClip(SfxFolder + "/UiConfirm.wav");
        managerSo.FindProperty("uiBackClip").objectReferenceValue = LoadClip(SfxFolder + "/UiBack.wav");
        managerSo.FindProperty("selectionMoveClip").objectReferenceValue = LoadClip(SfxFolder + "/SelectionMove.wav");
        managerSo.FindProperty("weaponFireClip").objectReferenceValue = LoadClip(SfxFolder + "/WeaponFire.wav");
        managerSo.FindProperty("inkImpactClip").objectReferenceValue = LoadClip(SfxFolder + "/InkImpact.wav");
        managerSo.FindProperty("matchStartClip").objectReferenceValue = LoadClip(SfxFolder + "/MatchStart.wav");
        managerSo.FindProperty("matchEndClip").objectReferenceValue = LoadClip(SfxFolder + "/MatchEnd.wav");
        managerSo.FindProperty("specialBurstClip").objectReferenceValue = LoadClip(SfxFolder + "/SpecialBurst.wav");
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, AudioManagerPrefabPath);

        if (loadedPrefabContents)
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }
    }

    private static AudioClip LoadClip(string path)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioSource GetOrCreateAudioSource(Transform parent, string name, bool loop)
    {
        Transform existing = parent.Find(name);
        GameObject sourceObject = existing != null ? existing.gameObject : new GameObject(name);
        sourceObject.transform.SetParent(parent, false);

        AudioSource source = GetOrAddComponent<AudioSource>(sourceObject);
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = loop ? 64 : 96;
        return source;
    }

    private static void UpdateMainMenuAudioControls()
    {
        if (!File.Exists(MainMenuCanvasPrefabPath))
        {
            Debug.LogWarning($"Main menu prefab is missing at {MainMenuCanvasPrefabPath}.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(MainMenuCanvasPrefabPath);
        MainMenuView view = root.GetComponent<MainMenuView>();

        if (view == null)
        {
            Debug.LogWarning("Main menu prefab does not contain MainMenuView.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        SerializedObject viewSo = new SerializedObject(view);
        GameObject settingsPanel = viewSo.FindProperty("settingsPanelObject").objectReferenceValue as GameObject;

        if (settingsPanel == null)
        {
            Debug.LogWarning("Main menu settings panel reference is missing.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        Transform settingsTransform = settingsPanel.transform;
        RemoveAudioRows(settingsTransform);
        RepositionSettingsControls(settingsTransform);

        Slider masterSlider = CreateVolumeRow(settingsTransform, "MasterVolumeRow", "Master Volume", 92f, new Color(0.05f, 0.85f, 1f, 1f), out Text masterValueText);
        Slider musicSlider = CreateVolumeRow(settingsTransform, "MusicVolumeRow", "Music Volume", 36f, new Color(1f, 0.32f, 0.74f, 1f), out Text musicValueText);
        Slider sfxSlider = CreateVolumeRow(settingsTransform, "SfxVolumeRow", "SFX Volume", -20f, new Color(1f, 0.78f, 0.12f, 1f), out Text sfxValueText);

        SetObjectReference(viewSo, "masterVolumeSlider", masterSlider);
        SetObjectReference(viewSo, "musicVolumeSlider", musicSlider);
        SetObjectReference(viewSo, "sfxVolumeSlider", sfxSlider);
        SetObjectReference(viewSo, "masterVolumeValueText", masterValueText);
        SetObjectReference(viewSo, "musicVolumeValueText", musicValueText);
        SetObjectReference(viewSo, "sfxVolumeValueText", sfxValueText);
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, MainMenuCanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void RemoveAudioRows(Transform settingsTransform)
    {
        string[] rowNames = { "MasterVolumeRow", "MusicVolumeRow", "SfxVolumeRow" };

        for (int i = 0; i < rowNames.Length; i++)
        {
            Transform row = settingsTransform.Find(rowNames[i]);

            if (row != null)
            {
                Object.DestroyImmediate(row.gameObject);
            }
        }
    }

    private static void RepositionSettingsControls(Transform settingsTransform)
    {
        SetAnchoredPosition(settingsTransform, "SettingsTitle", new Vector2(0f, 218f));
        SetAnchoredPosition(settingsTransform, "SettingsSummaryText", new Vector2(0f, 156f));
        SetAnchoredPosition(settingsTransform, "FullscreenButton", new Vector2(0f, -86f));
        SetAnchoredPosition(settingsTransform, "PerformantButton", new Vector2(-230f, -154f));
        SetAnchoredPosition(settingsTransform, "BalancedButton", new Vector2(0f, -154f));
        SetAnchoredPosition(settingsTransform, "HighFidelityButton", new Vector2(230f, -154f));
        SetAnchoredPosition(settingsTransform, "SettingsBackButton", new Vector2(0f, -238f));
    }

    private static void SetAnchoredPosition(Transform parent, string childName, Vector2 anchoredPosition)
    {
        Transform child = parent.Find(childName);
        RectTransform rectTransform = child != null ? child.GetComponent<RectTransform>() : null;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    private static Slider CreateVolumeRow(Transform parent, string rowName, string label, float y, Color accentColor, out Text valueText)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);
        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(660f, 44f);

        Text labelText = CreateText("Label", rowObject.transform, label, 22, TextAnchor.MiddleLeft, Color.white);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        labelRect.sizeDelta = new Vector2(190f, 38f);

        GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(Image));
        trackObject.transform.SetParent(rowObject.transform, false);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(0f, 0.5f);
        trackRect.pivot = new Vector2(0f, 0.5f);
        trackRect.anchoredPosition = new Vector2(210f, 0f);
        trackRect.sizeDelta = new Vector2(300f, 12f);
        Image trackImage = trackObject.GetComponent<Image>();
        trackImage.color = new Color(0.08f, 0.1f, 0.13f, 0.95f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(trackObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 0f);
        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = accentColor;

        GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(trackObject.transform, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(26f, 26f);
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.color = Color.white;

        valueText = CreateText("Value", rowObject.transform, "100%", 21, TextAnchor.MiddleRight, Color.white);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0f, 0f);
        valueRect.sizeDelta = new Vector2(82f, 38f);

        Slider slider = rowObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.85f;
        slider.wholeNumbers = false;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        Navigation navigation = slider.navigation;
        navigation.mode = Navigation.Mode.Automatic;
        slider.navigation = navigation;

        ColorBlock colors = slider.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        colors.pressedColor = accentColor;
        colors.selectedColor = Color.white;
        slider.colors = colors;

        return slider;
    }

    private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        Text textComponent = textObject.GetComponent<Text>();
        textComponent.text = text;
        textComponent.color = color;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = FontStyle.Bold;
        textComponent.alignment = alignment;
        textComponent.raycastTarget = false;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return textComponent;
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
