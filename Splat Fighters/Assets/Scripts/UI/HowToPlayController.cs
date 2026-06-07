using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Switches between prefab-authored How to Play pages without owning the page content.
/// </summary>
[DisallowMultipleComponent]
public sealed class HowToPlayController : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons = null;
    [SerializeField] private GameObject[] pageObjects = null;
    [SerializeField] private Text pageCounterText = null;
    [SerializeField] private Button trainingButton = null;
    [SerializeField] private string trainingSceneName = "HowToPlayTraining";
    [SerializeField] private Color selectedTabColor = new Color(0.05f, 0.72f, 0.95f, 0.98f);
    [SerializeField] private Color idleTabColor = new Color(0.11f, 0.13f, 0.17f, 0.94f);

    private int selectedPageIndex;
    private bool handlersRegistered;
    private bool trainingButtonRegistered;

    private void Awake()
    {
        RegisterTabHandlers();
        RegisterTrainingButton();
    }

    private void OnEnable()
    {
        RegisterTabHandlers();
        RegisterTrainingButton();
        ShowPage(0, false);
    }

    public void ShowPage(int pageIndex)
    {
        ShowPage(pageIndex, true);
    }

    private void ShowPage(int pageIndex, bool playSound)
    {
        if (pageObjects == null || pageObjects.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(pageIndex, 0, pageObjects.Length - 1);
        bool changed = clampedIndex != selectedPageIndex;
        selectedPageIndex = clampedIndex;

        for (int i = 0; i < pageObjects.Length; i++)
        {
            if (pageObjects[i] != null)
            {
                pageObjects[i].SetActive(i == selectedPageIndex);
            }
        }

        RefreshTabVisuals();
        RefreshPageCounter();

        if (playSound && changed)
        {
            SplatAudioManager.PlaySelectionMoveSound();
        }
    }

    private void RegisterTabHandlers()
    {
        if (handlersRegistered || tabButtons == null)
        {
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            Button button = tabButtons[i];

            if (button == null)
            {
                continue;
            }

            int pageIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowPage(pageIndex));
            SetButtonTextColor(button, Color.white);
        }

        handlersRegistered = true;
    }

    private void RegisterTrainingButton()
    {
        if (trainingButtonRegistered || trainingButton == null)
        {
            return;
        }

        trainingButton.onClick.RemoveAllListeners();
        trainingButton.onClick.AddListener(StartTrainingScene);
        SetButtonTextColor(trainingButton, Color.white);
        trainingButtonRegistered = true;
    }

    private void RefreshTabVisuals()
    {
        if (tabButtons == null)
        {
            return;
        }

        for (int i = 0; i < tabButtons.Length; i++)
        {
            Button button = tabButtons[i];

            if (button == null)
            {
                continue;
            }

            bool selected = i == selectedPageIndex;
            Color tabColor = selected ? selectedTabColor : idleTabColor;
            Image image = button.GetComponent<Image>();

            if (image != null)
            {
                image.color = tabColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = tabColor;
            colors.highlightedColor = tabColor;
            colors.selectedColor = tabColor;
            colors.pressedColor = new Color(Mathf.Max(tabColor.r - 0.08f, 0f), Mathf.Max(tabColor.g - 0.08f, 0f), Mathf.Max(tabColor.b - 0.08f, 0f), tabColor.a);
            button.colors = colors;
            SetButtonTextColor(button, Color.white);
        }
    }

    private void RefreshPageCounter()
    {
        if (pageCounterText != null && pageObjects != null && pageObjects.Length > 0)
        {
            pageCounterText.text = $"{selectedPageIndex + 1}/{pageObjects.Length}";
        }
    }

    private static void SetButtonTextColor(Button button, Color color)
    {
        foreach (Text text in button.GetComponentsInChildren<Text>(true))
        {
            text.color = color;
        }
    }

    private void StartTrainingScene()
    {
        SplatAudioManager.PlayUiConfirmSound();
        SceneManager.LoadScene(trainingSceneName, LoadSceneMode.Single);
    }
}
