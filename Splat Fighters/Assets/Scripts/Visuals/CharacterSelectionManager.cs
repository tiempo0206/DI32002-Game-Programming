using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Auto-wires imported RPG Monster characters into the MVP scene and lets the player cycle them at runtime.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(160)]
public sealed class CharacterSelectionManager : MonoBehaviour
{
    public const string PlayerCharacterPrefsKey = "SplatFighters.SelectedPlayerCharacter";
    public const string OpponentCharacterPrefsKey = "SplatFighters.SelectedOpponentCharacter";

    private const string LegacyCharacterPrefsKey = "SplatFighters.SelectedCharacter";
    private const string GameplaySceneName = "MVP_ShootingTest";

    [SerializeField] private KeyCode nextCharacterKey = KeyCode.C;
    [SerializeField] private KeyCode previousCharacterKey = KeyCode.V;
    [SerializeField] private CharacterVisualCatalog catalog = null;

    private CharacterVisualController playerVisual;
    private CharacterVisualController botVisual;
    private PlayerController playerController;
    private BotController botController;
    private Text selectionText;
    private int selectedIndex;
    private int opponentSelectedIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapActiveScene()
    {
        EnsureManagerForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManagerForScene(scene);
    }

    private static void EnsureManagerForScene(Scene scene)
    {
        if (scene.name != GameplaySceneName || FindObjectOfType<CharacterSelectionManager>() != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("CharacterSelectionManager");
        managerObject.AddComponent<CharacterSelectionManager>();
    }

    private void Awake()
    {
        if (catalog == null)
        {
            catalog = CharacterVisualCatalog.LoadDefault();
        }

        int legacyIndex = PlayerPrefs.GetInt(LegacyCharacterPrefsKey, 5);
        selectedIndex = PlayerPrefs.GetInt(PlayerCharacterPrefsKey, legacyIndex);
        opponentSelectedIndex = PlayerPrefs.GetInt(OpponentCharacterPrefsKey, selectedIndex + 1);

        if (catalog != null && catalog.Count > 0)
        {
            selectedIndex = catalog.NormalizeIndex(selectedIndex);
            opponentSelectedIndex = EnsureDistinctOpponentIndex(opponentSelectedIndex);
            SaveSelectedCharactersAndColors();
        }
    }

    private void Start()
    {
        EnsureBindings();
        EnsureSelectionUi();
        RefreshSelectionText();
    }

    private void Update()
    {
        EnsureBindings();

        if (catalog == null || catalog.Count == 0 || playerVisual == null)
        {
            RefreshSelectionText();
            return;
        }

        if (Input.GetKeyDown(nextCharacterKey))
        {
            SelectCharacter(selectedIndex + 1);
        }
        else if (Input.GetKeyDown(previousCharacterKey))
        {
            SelectCharacter(selectedIndex - 1);
        }

        if (playerController != null && playerVisual != null)
        {
            playerVisual.SetSwimming(playerController.IsSwimming);
        }

        RefreshSelectionText();
    }

    public void SelectCharacter(int index)
    {
        if (catalog == null || catalog.Count == 0)
        {
            return;
        }

        selectedIndex = catalog.NormalizeIndex(index);

        if (catalog.Count > 1 && opponentSelectedIndex == selectedIndex)
        {
            opponentSelectedIndex = EnsureDistinctOpponentIndex(opponentSelectedIndex + 1);

            if (botVisual != null)
            {
                botVisual.Select(opponentSelectedIndex);
            }
        }

        SaveSelectedCharactersAndColors();

        if (playerVisual != null)
        {
            playerVisual.Select(selectedIndex);
        }
    }

    private void EnsureBindings()
    {
        if (catalog == null)
        {
            catalog = CharacterVisualCatalog.LoadDefault();
        }

        if (catalog == null || catalog.Count == 0)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController != null && playerVisual == null)
        {
            playerVisual = AttachVisualController(playerController.gameObject, playerController.PlayerTeam, selectedIndex);
        }

        if (botController == null)
        {
            botController = FindObjectOfType<BotController>();
        }

        if (botController != null && botVisual == null)
        {
            botVisual = AttachVisualController(botController.gameObject, botController.BotTeam, opponentSelectedIndex);
        }
    }

    private CharacterVisualController AttachVisualController(GameObject target, Team team, int index)
    {
        CharacterVisualController visualController = target.GetComponent<CharacterVisualController>();

        if (visualController == null)
        {
            visualController = target.AddComponent<CharacterVisualController>();
        }

        visualController.Configure(catalog, team, index);
        return visualController;
    }

    private int EnsureDistinctOpponentIndex(int index)
    {
        int normalized = catalog.NormalizeIndex(index);
        return catalog.Count > 1 && normalized == selectedIndex
            ? catalog.NormalizeIndex(normalized + 1)
            : normalized;
    }

    private void SaveSelectedCharactersAndColors()
    {
        PlayerPrefs.SetInt(PlayerCharacterPrefsKey, selectedIndex);
        PlayerPrefs.SetInt(OpponentCharacterPrefsKey, opponentSelectedIndex);
        SaveSelectedInkColor(Team.TeamA, selectedIndex);
        SaveSelectedInkColor(Team.TeamB, opponentSelectedIndex);
        PlayerPrefs.Save();
    }

    private void SaveSelectedInkColor(Team team, int index)
    {
        CharacterVisualOption option = catalog.GetOption(index);
        if (option != null)
        {
            TeamVisualPalette.SaveSelectedColor(team, option.InkColor);
        }
    }

    private void EnsureSelectionUi()
    {
        if (selectionText != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeHudCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject textObject = new GameObject("CharacterSelectionText");
        textObject.transform.SetParent(canvas.transform, false);
        selectionText = textObject.AddComponent<Text>();
        selectionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        selectionText.fontSize = 16;
        selectionText.alignment = TextAnchor.UpperRight;
        selectionText.color = Color.white;
        selectionText.horizontalOverflow = HorizontalWrapMode.Overflow;
        selectionText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectTransform = selectionText.rectTransform;
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-18f, -18f);
        rectTransform.sizeDelta = new Vector2(360f, 48f);
    }

    private void RefreshSelectionText()
    {
        if (selectionText == null)
        {
            return;
        }

        if (catalog == null || catalog.Count == 0 || playerVisual == null)
        {
            selectionText.text = "Character catalog is loading...";
            selectionText.color = new Color(1f, 0.78f, 0.28f, 1f);
            return;
        }

        selectionText.color = TeamVisualPalette.GetColor(Team.TeamA);
        selectionText.text = $"Character: {playerVisual.CurrentDisplayName}   C/V to cycle";
    }
}
