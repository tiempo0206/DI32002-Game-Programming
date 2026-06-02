using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-wires imported RPG Monster characters into the MVP scene and lets the player cycle them at runtime.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(160)]
public sealed class CharacterSelectionManager : MonoBehaviour
{
    private const string PlayerPrefsKey = "SplatFighters.SelectedCharacter";

    [SerializeField] private KeyCode nextCharacterKey = KeyCode.C;
    [SerializeField] private KeyCode previousCharacterKey = KeyCode.V;
    [SerializeField] private CharacterVisualCatalog catalog = null;

    private CharacterVisualController playerVisual;
    private CharacterVisualController botVisual;
    private PlayerController playerController;
    private BotController botController;
    private Text selectionText;
    private int selectedIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<CharacterSelectionManager>() != null)
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

        selectedIndex = PlayerPrefs.GetInt(PlayerPrefsKey, 5);
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
        PlayerPrefs.SetInt(PlayerPrefsKey, selectedIndex);

        if (playerVisual != null)
        {
            playerVisual.Select(selectedIndex);
        }

        if (botVisual != null)
        {
            botVisual.Select(selectedIndex + 1);
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
            botVisual = AttachVisualController(botController.gameObject, botController.BotTeam, selectedIndex + 1);
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
