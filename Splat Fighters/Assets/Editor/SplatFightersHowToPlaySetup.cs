using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor utility that rebuilds the How to Play panel as prefab-authored tutorial pages.
/// </summary>
public static class SplatFightersHowToPlaySetup
{
    private const string MainMenuCanvasPrefabPath = "Assets/Resources/UI/MainMenu/Prefabs/MainMenuCanvas.prefab";

    [MenuItem("Tools/Splat Fighters/Apply How To Play Presentation Setup")]
    public static void ApplyHowToPlayPresentationSetup()
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
        GameObject instructionsPanel = viewSo.FindProperty("instructionsPanelObject").objectReferenceValue as GameObject;

        if (instructionsPanel == null)
        {
            Debug.LogWarning("Main menu instructions panel reference is missing.");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        BuildHowToPlayPanel(instructionsPanel, viewSo);
        viewSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, MainMenuCanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied upgraded How to Play tutorial panel to MainMenuCanvas.prefab.");
    }

    private static void BuildHowToPlayPanel(GameObject panelObject, SerializedObject viewSo)
    {
        ConfigurePanel(panelObject);
        RemoveChildren(panelObject.transform);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        HowToPlayController controller = GetOrAddComponent<HowToPlayController>(panelObject);

        CreateInkStripe(panelRect, "CyanTopStripe", new Vector2(-390f, 286f), new Vector2(310f, 12f), new Color(0.02f, 0.86f, 1f, 0.95f));
        CreateInkStripe(panelRect, "PinkTopStripe", new Vector2(-80f, 286f), new Vector2(210f, 12f), new Color(1f, 0.06f, 0.53f, 0.95f));
        CreateInkStripe(panelRect, "YellowTopStripe", new Vector2(210f, 286f), new Vector2(310f, 12f), new Color(1f, 0.78f, 0.08f, 0.95f));

        CreateText("InstructionsTitle", panelRect, "How to Play", new Vector2(0f, 248f), new Vector2(820f, 58f), 46, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        CreateText("InstructionsSubtitle", panelRect, "Ink the arena. Control the fight. Finish with more turf.", new Vector2(0f, 209f), new Vector2(780f, 34f), 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.88f, 0.95f, 1f, 1f));

        List<Button> tabButtons = CreateTabs(panelRect);
        List<GameObject> pages = CreatePages(panelRect);
        Text pageCounterText = CreateText("PageCounter", panelRect, "1/5", new Vector2(404f, -260f), new Vector2(88f, 32f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Button trainingButton = CreateButton(panelRect, "TrainingSceneButton", "Start Training", new Vector2(-132f, -260f), new Vector2(224f, 58f), new Color(0.05f, 0.62f, 0.86f, 0.97f));
        Button backButton = CreateButton(panelRect, "InstructionsBackButton", "Back", new Vector2(132f, -260f), new Vector2(194f, 58f), new Color(0.17f, 0.18f, 0.23f, 0.97f));

        SerializedObject controllerSo = new SerializedObject(controller);
        SetObjectArray(controllerSo, "tabButtons", tabButtons);
        SetGameObjectArray(controllerSo, "pageObjects", pages);
        SetObjectReference(controllerSo, "pageCounterText", pageCounterText);
        SetObjectReference(controllerSo, "trainingButton", trainingButton);
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        SetObjectReference(viewSo, "instructionsBackButton", backButton);
    }

    private static void ConfigurePanel(GameObject panelObject)
    {
        RectTransform rect = GetOrAddComponent<RectTransform>(panelObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1040f, 650f);

        Image image = GetOrAddComponent<Image>(panelObject);
        image.color = new Color(0.025f, 0.031f, 0.046f, 0.91f);
        image.raycastTarget = true;

        CanvasGroup group = GetOrAddComponent<CanvasGroup>(panelObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private static List<Button> CreateTabs(Transform parent)
    {
        string[] labels =
        {
            "Basics",
            "Controls",
            "Ink & Combat",
            "Game Modes",
            "Tips"
        };

        List<Button> buttons = new List<Button>();
        const float startX = -392f;
        const float stepX = 196f;

        for (int i = 0; i < labels.Length; i++)
        {
            Color color = i == 0 ? new Color(0.05f, 0.72f, 0.95f, 0.98f) : new Color(0.11f, 0.13f, 0.17f, 0.94f);
            buttons.Add(CreateButton(parent, $"InstructionsTab_{labels[i].Replace(" ", string.Empty).Replace("&", "And")}", labels[i], new Vector2(startX + stepX * i, 158f), new Vector2(178f, 42f), color, 19));
        }

        return buttons;
    }

    private static List<GameObject> CreatePages(Transform parent)
    {
        List<GameObject> pages = new List<GameObject>
        {
            CreateBasicsPage(parent),
            CreateControlsPage(parent),
            CreateInkCombatPage(parent),
            CreateModesPage(parent),
            CreateTipsPage(parent)
        };

        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == 0);
        }

        return pages;
    }

    private static GameObject CreateBasicsPage(Transform parent)
    {
        GameObject page = CreatePageRoot(parent, "BasicsPage");

        CreateInfoCard(page.transform, "BasicsObjective", "MATCH FLOW", "Pick a mode and fighters, then enter a 180 second ink match.", new Vector2(-270f, 40f), new Vector2(430f, 190f), new Color(0.04f, 0.58f, 0.82f, 0.9f));
        CreateInfoCard(page.transform, "BasicsGoal", "MAIN GOAL", "Paint ground with your team's ink while denying space to the rival team.", new Vector2(270f, 40f), new Vector2(430f, 190f), new Color(0.93f, 0.08f, 0.48f, 0.9f));
        CreateWideNote(page.transform, "BasicsWin", "WIN CHECK", "At the timer, the team with the higher total ink coverage wins the round.", new Vector2(0f, -115f), new Color(1f, 0.78f, 0.08f, 0.92f));
        return page;
    }

    private static GameObject CreateControlsPage(Transform parent)
    {
        GameObject page = CreatePageRoot(parent, "ControlsPage");

        CreateKeyBlock(page.transform, "MoveKeys", "WASD", "Move", new Vector2(-360f, 78f), new Color(0.03f, 0.74f, 0.9f, 0.92f));
        CreateKeyBlock(page.transform, "AimKey", "Mouse", "Aim camera", new Vector2(-120f, 78f), new Color(0.93f, 0.08f, 0.48f, 0.92f));
        CreateKeyBlock(page.transform, "FireKey", "LMB", "Fire ink", new Vector2(120f, 78f), new Color(1f, 0.72f, 0.05f, 0.92f));
        CreateKeyBlock(page.transform, "JumpKey", "Space", "Jump", new Vector2(360f, 78f), new Color(0.36f, 0.86f, 0.17f, 0.92f));
        CreateKeyBlock(page.transform, "SwimKey", "Shift", "Swim ink", new Vector2(-240f, -92f), new Color(0.58f, 0.25f, 0.95f, 0.92f));
        CreateKeyBlock(page.transform, "SpecialKey", "Q", "Special burst", new Vector2(0f, -92f), new Color(0.02f, 0.86f, 1f, 0.92f));
        CreateKeyBlock(page.transform, "ToolKeys", "1 / 2", "Shooter / Roller", new Vector2(240f, -92f), new Color(1f, 0.28f, 0.72f, 0.92f));
        CreateText("PauseHint", page.transform, "Pause with P or Esc", new Vector2(0f, -177f), new Vector2(520f, 30f), 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 1f, 1f));
        return page;
    }

    private static GameObject CreateInkCombatPage(Transform parent)
    {
        GameObject page = CreatePageRoot(parent, "InkCombatPage");

        CreateInfoCard(page.transform, "PaintCard", "PAINT", "Every shot claims ground and builds map control. Keep lanes painted before chasing fights.", new Vector2(-310f, 42f), new Vector2(360f, 214f), new Color(0.02f, 0.74f, 0.92f, 0.9f));
        CreateInfoCard(page.transform, "CombatCard", "COMBAT", "Ink shots pressure opponents. Stay mobile, fire in bursts, and avoid standing in enemy ink.", new Vector2(0f, 42f), new Vector2(360f, 214f), new Color(1f, 0.08f, 0.5f, 0.9f));
        CreateInfoCard(page.transform, "SpecialCard", "SPECIAL", "Use the special burst when fights collapse around the tower, a choke, or a tight lane.", new Vector2(310f, 42f), new Vector2(360f, 214f), new Color(1f, 0.75f, 0.08f, 0.9f));
        CreateWideNote(page.transform, "InkCombatNote", "INK MOVEMENT", "Your ink is a path. Enemy ink is pressure. Repaint escape routes before committing.", new Vector2(0f, -132f), new Color(0.36f, 0.86f, 0.17f, 0.9f));
        return page;
    }

    private static GameObject CreateModesPage(Transform parent)
    {
        GameObject page = CreatePageRoot(parent, "GameModesPage");

        CreateModeCard(page.transform, "TurfWarCard", "TURF WAR", "Cover more total ground before time runs out.", new Vector2(-220f, 36f), new Color(0.02f, 0.77f, 0.92f, 0.92f));
        CreateModeCard(page.transform, "TowerControlCard", "TOWER CONTROL", "Paint near the tower to push it. First to the goal wins; otherwise the best tower push wins.", new Vector2(220f, 36f), new Color(1f, 0.75f, 0.08f, 0.92f));
        CreateText("ModesNote", page.transform, "Tower Control is decided by tower goal, tower lead, tower control, then local tower paint advantage.", new Vector2(0f, -158f), new Vector2(850f, 48f), 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.96f, 1f, 1f));
        return page;
    }

    private static GameObject CreateTipsPage(Transform parent)
    {
        GameObject page = CreatePageRoot(parent, "TipsPage");

        CreateTipRow(page.transform, "TipOne", "1", "Paint exits before fighting so you always have a route out.", new Vector2(0f, 100f), new Color(0.02f, 0.78f, 0.96f, 0.94f));
        CreateTipRow(page.transform, "TipTwo", "2", "Use shooter pressure for range and roller paths for fast turf control.", new Vector2(0f, 38f), new Color(1f, 0.08f, 0.5f, 0.94f));
        CreateTipRow(page.transform, "TipThree", "3", "Save special burst for a tower fight, crowded choke, or final push.", new Vector2(0f, -24f), new Color(1f, 0.75f, 0.08f, 0.94f));
        CreateTipRow(page.transform, "TipFour", "4", "Switch fighters to find a readable ink color and silhouette.", new Vector2(0f, -86f), new Color(0.35f, 0.88f, 0.18f, 0.94f));
        CreateText("TipsFooter", page.transform, "Move first, paint second, duel only when the space is yours.", new Vector2(0f, -165f), new Vector2(840f, 34f), 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        return page;
    }

    private static GameObject CreatePageRoot(Transform parent, string name)
    {
        GameObject pageObject = new GameObject(name, typeof(RectTransform));
        pageObject.transform.SetParent(parent, false);

        RectTransform rect = pageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(930f, 330f);
        return pageObject;
    }

    private static void CreateInfoCard(Transform parent, string name, string title, string body, Vector2 anchoredPosition, Vector2 size, Color accentColor)
    {
        GameObject cardObject = CreateImageObject(parent, name, new Color(0.055f, 0.064f, 0.085f, 0.95f), anchoredPosition, size);
        CreateInkStripe(cardObject.transform, $"{name}Accent", new Vector2(0f, size.y * 0.5f - 8f), new Vector2(size.x - 26f, 10f), accentColor);
        CreateText($"{name}Title", cardObject.transform, title, new Vector2(0f, size.y * 0.5f - 46f), new Vector2(size.x - 46f, 36f), 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        CreateText($"{name}Body", cardObject.transform, body, new Vector2(0f, -18f), new Vector2(size.x - 46f, size.y - 82f), 21, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.91f, 0.96f, 1f, 1f));
    }

    private static void CreateModeCard(Transform parent, string name, string title, string body, Vector2 anchoredPosition, Color accentColor)
    {
        CreateInfoCard(parent, name, title, body, anchoredPosition, new Vector2(282f, 224f), accentColor);
    }

    private static void CreateWideNote(Transform parent, string name, string title, string body, Vector2 anchoredPosition, Color accentColor)
    {
        GameObject rowObject = CreateImageObject(parent, name, new Color(0.06f, 0.07f, 0.09f, 0.96f), anchoredPosition, new Vector2(880f, 78f));
        CreateInkStripe(rowObject.transform, $"{name}Accent", new Vector2(-418f, 0f), new Vector2(12f, 54f), accentColor);
        CreateText($"{name}Title", rowObject.transform, title, new Vector2(-314f, 0f), new Vector2(170f, 46f), 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        CreateText($"{name}Body", rowObject.transform, body, new Vector2(126f, 0f), new Vector2(636f, 46f), 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.91f, 0.96f, 1f, 1f));
    }

    private static void CreateKeyBlock(Transform parent, string name, string keyLabel, string actionLabel, Vector2 anchoredPosition, Color accentColor)
    {
        GameObject blockObject = CreateImageObject(parent, name, new Color(0.055f, 0.064f, 0.085f, 0.95f), anchoredPosition, new Vector2(202f, 126f));
        CreateInkStripe(blockObject.transform, $"{name}Accent", new Vector2(0f, 52f), new Vector2(174f, 9f), accentColor);
        CreateText($"{name}Key", blockObject.transform, keyLabel, new Vector2(0f, 17f), new Vector2(164f, 40f), 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        CreateText($"{name}Action", blockObject.transform, actionLabel, new Vector2(0f, -29f), new Vector2(170f, 34f), 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.91f, 0.96f, 1f, 1f));
    }

    private static void CreateTipRow(Transform parent, string name, string number, string body, Vector2 anchoredPosition, Color accentColor)
    {
        GameObject rowObject = CreateImageObject(parent, name, new Color(0.055f, 0.064f, 0.085f, 0.95f), anchoredPosition, new Vector2(760f, 50f));
        GameObject numberObject = CreateImageObject(rowObject.transform, $"{name}NumberBlock", accentColor, new Vector2(-337f, 0f), new Vector2(54f, 34f));
        CreateText($"{name}Number", numberObject.transform, number, Vector2.zero, new Vector2(48f, 30f), 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        CreateText($"{name}Body", rowObject.transform, body, new Vector2(58f, 0f), new Vector2(610f, 34f), 20, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.97f, 1f, 1f));
    }

    private static GameObject CreateImageObject(Transform parent, string name, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return imageObject;
    }

    private static void CreateInkStripe(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        CreateImageObject(parent, name, color, anchoredPosition, size);
    }

    private static Text CreateText(string name, Transform parent, string text, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text textComponent = textObject.GetComponent<Text>();
        textComponent.text = text;
        textComponent.color = color;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = alignment;
        textComponent.raycastTarget = false;
        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Truncate;
        textComponent.resizeTextForBestFit = true;
        textComponent.resizeTextMinSize = 10;
        textComponent.resizeTextMaxSize = fontSize;

        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.58f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        return textComponent;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        return CreateButton(parent, name, label, anchoredPosition, size, color, 22);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize)
    {
        GameObject buttonObject = CreateImageObject(parent, name, color, anchoredPosition, size);
        Button button = buttonObject.AddComponent<Button>();
        Image targetImage = buttonObject.GetComponent<Image>();
        button.targetGraphic = targetImage;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = new Color(Mathf.Min(color.r + 0.08f, 1f), Mathf.Min(color.g + 0.08f, 1f), Mathf.Min(color.b + 0.08f, 1f), color.a);
        colors.pressedColor = new Color(Mathf.Max(color.r - 0.08f, 0f), Mathf.Max(color.g - 0.08f, 0f), Mathf.Max(color.b - 0.08f, 0f), color.a);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.14f, 0.16f, 0.2f, 0.6f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        Text buttonText = CreateText("Label", buttonObject.transform, label, Vector2.zero, size, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        buttonText.resizeTextMinSize = 9;
        return button;
    }

    private static void RemoveChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectArray<T>(SerializedObject serializedObject, string propertyName, IReadOnlyList<T> values) where T : Object
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            return;
        }

        property.arraySize = values.Count;

        for (int i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void SetGameObjectArray(SerializedObject serializedObject, string propertyName, IReadOnlyList<GameObject> values)
    {
        SetObjectArray(serializedObject, propertyName, values);
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
