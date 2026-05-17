using UnityEngine;

/// <summary>
/// Small local-demo tool selector for switching between the default shooter and roller prototype.
/// </summary>
[DisallowMultipleComponent]
public class PlayerToolSwitcher : MonoBehaviour
{
    public enum ToolMode
    {
        Shooter,
        Roller
    }

    [Header("Tools")]
    [SerializeField] private ToolMode defaultTool = ToolMode.Shooter;
    [SerializeField] private ToolMode currentTool = ToolMode.Shooter;
    [SerializeField] private InkWeapon shooter = null;
    [SerializeField] private RollerPaintTool roller = null;

    [Header("Input")]
    [SerializeField] private bool enableKeyboardSwitching = true;
    [SerializeField] private KeyCode shooterKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode rollerKey = KeyCode.Alpha2;

    [Header("Visuals")]
    [SerializeField] private Renderer[] rollerRenderers = new Renderer[0];

    public ToolMode CurrentTool => currentTool;
    public bool IsShooterActive => currentTool == ToolMode.Shooter;
    public bool IsRollerActive => currentTool == ToolMode.Roller;
    public string CurrentToolLabel => currentTool == ToolMode.Roller ? "Roller" : "Shooter";

    private void Awake()
    {
        ResolveReferences();
        SetTool(defaultTool);
    }

    private void Update()
    {
        if (!enableKeyboardSwitching)
        {
            return;
        }

        if (Input.GetKeyDown(shooterKey))
        {
            SetTool(ToolMode.Shooter);
        }
        else if (Input.GetKeyDown(rollerKey))
        {
            SetTool(ToolMode.Roller);
        }
    }

    public void ResetToDefaultTool()
    {
        SetTool(defaultTool);
    }

    public void SetTool(ToolMode nextTool)
    {
        currentTool = nextTool;
        ApplyToolState();
    }

    private void ResolveReferences()
    {
        if (shooter == null)
        {
            shooter = GetComponentInChildren<InkWeapon>(true);
        }

        if (roller == null)
        {
            roller = GetComponentInChildren<RollerPaintTool>(true);
        }

        if ((rollerRenderers == null || rollerRenderers.Length == 0) && roller != null)
        {
            rollerRenderers = roller.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void ApplyToolState()
    {
        bool rollerActive = currentTool == ToolMode.Roller;

        if (roller != null)
        {
            roller.enabled = rollerActive;
        }

        if (shooter != null)
        {
            shooter.SetExternalFireBlocked(rollerActive);
        }

        if (rollerRenderers == null)
        {
            return;
        }

        for (int i = 0; i < rollerRenderers.Length; i++)
        {
            Renderer rollerRenderer = rollerRenderers[i];

            if (rollerRenderer != null)
            {
                rollerRenderer.enabled = rollerActive;
            }
        }
    }
}
