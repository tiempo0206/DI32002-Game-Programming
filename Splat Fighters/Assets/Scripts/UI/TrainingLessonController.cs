using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Presents a lightweight interactive lesson inside the training scene.
/// </summary>
[DisallowMultipleComponent]
public sealed class TrainingLessonController : MonoBehaviour
{
    private enum LessonStep
    {
        Move,
        Paint,
        Swim,
        Special,
        Complete
    }

    [SerializeField] private PlayerController playerController = null;
    [SerializeField] private Transform playerTransform = null;
    [SerializeField] private PaintManager paintManager = null;
    [SerializeField] private SpecialMeter specialMeter = null;
    [SerializeField] private Text titleText = null;
    [SerializeField] private Text bodyText = null;
    [SerializeField] private Text progressText = null;
    [SerializeField] private Button backToMenuButton = null;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Min(0.5f)] private float movementGoalDistance = 5f;
    [SerializeField, Range(1f, 100f)] private float paintGoalPercent = 12f;
    [SerializeField, Min(0.1f)] private float swimGoalSeconds = 1.2f;

    private LessonStep currentStep;
    private Vector3 movementStartPosition;
    private float swimHeldSeconds;
    private bool specialPromptSeen;

    private void Awake()
    {
        ResolveReferences();
        ConfigureButton();
        movementStartPosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        RefreshView();
    }

    private void Update()
    {
        ResolveReferences();
        UpdateLessonState();
        RefreshView();
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerTransform == null && playerController != null)
        {
            playerTransform = playerController.transform;
        }

        if (paintManager == null)
        {
            paintManager = PaintManager.Instance != null ? PaintManager.Instance : FindObjectOfType<PaintManager>();
        }

        if (specialMeter == null && playerTransform != null)
        {
            specialMeter = playerTransform.GetComponentInChildren<SpecialMeter>();
        }
    }

    private void ConfigureButton()
    {
        if (backToMenuButton == null)
        {
            return;
        }

        backToMenuButton.onClick.RemoveAllListeners();
        backToMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void UpdateLessonState()
    {
        switch (currentStep)
        {
            case LessonStep.Move:
                if (GetMovedDistance() >= movementGoalDistance)
                {
                    currentStep = LessonStep.Paint;
                    SplatAudioManager.PlayUiConfirmSound();
                }
                break;
            case LessonStep.Paint:
                if (GetTeamACoverage() >= paintGoalPercent)
                {
                    currentStep = LessonStep.Swim;
                    SplatAudioManager.PlayUiConfirmSound();
                }
                break;
            case LessonStep.Swim:
                if (playerController != null && playerController.IsSwimming)
                {
                    swimHeldSeconds += Time.deltaTime;
                }

                if (swimHeldSeconds >= swimGoalSeconds)
                {
                    currentStep = LessonStep.Special;
                    SplatAudioManager.PlayUiConfirmSound();
                }
                break;
            case LessonStep.Special:
                if (specialMeter != null && specialMeter.IsReady)
                {
                    specialPromptSeen = true;
                }

                if (specialPromptSeen && Input.GetKeyDown(KeyCode.Q))
                {
                    currentStep = LessonStep.Complete;
                    SplatAudioManager.PlayUiConfirmSound();
                }
                break;
        }
    }

    private void RefreshView()
    {
        if (titleText != null)
        {
            titleText.text = GetTitle();
        }

        if (bodyText != null)
        {
            bodyText.text = GetBody();
        }

        if (progressText != null)
        {
            progressText.text = GetProgress();
        }
    }

    private string GetTitle()
    {
        switch (currentStep)
        {
            case LessonStep.Move:
                return "Step 1 - Move";
            case LessonStep.Paint:
                return "Step 2 - Paint";
            case LessonStep.Swim:
                return "Step 3 - Swim";
            case LessonStep.Special:
                return "Step 4 - Special";
            default:
                return "Training Complete";
        }
    }

    private string GetBody()
    {
        switch (currentStep)
        {
            case LessonStep.Move:
                return "Use WASD and mouse aim to move around the small arena.";
            case LessonStep.Paint:
                return "Hold Left Mouse Button to cover the floor with your ink.";
            case LessonStep.Swim:
                return "Stand on your own ink, then hold Left Shift to swim faster and refill ink.";
            case LessonStep.Special:
                return "Keep painting until the special meter is ready, then press Q near the center target.";
            default:
                return "You know the core loop now. Press Back to Menu or keep practicing in the arena.";
        }
    }

    private string GetProgress()
    {
        switch (currentStep)
        {
            case LessonStep.Move:
                return $"Movement: {Mathf.Min(GetMovedDistance(), movementGoalDistance):0.0}/{movementGoalDistance:0.0}m";
            case LessonStep.Paint:
                return $"Team ink coverage: {Mathf.Min(GetTeamACoverage(), paintGoalPercent):0.0}/{paintGoalPercent:0.0}%";
            case LessonStep.Swim:
                return $"Swim time: {Mathf.Min(swimHeldSeconds, swimGoalSeconds):0.0}/{swimGoalSeconds:0.0}s";
            case LessonStep.Special:
                return specialMeter != null && specialMeter.IsReady ? "Special ready: press Q" : $"Special charge: {(specialMeter != null ? specialMeter.ChargePercent : 0f):0}%";
            default:
                return "Basics cleared";
        }
    }

    private float GetMovedDistance()
    {
        if (playerTransform == null)
        {
            return 0f;
        }

        Vector3 flatDelta = playerTransform.position - movementStartPosition;
        flatDelta.y = 0f;
        return flatDelta.magnitude;
    }

    private float GetTeamACoverage()
    {
        return paintManager != null ? paintManager.GetCoveragePercent(Team.TeamA) : 0f;
    }

    private void ReturnToMainMenu()
    {
        SplatAudioManager.PlayUiBackSound();
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }
}
