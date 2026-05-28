using UnityEngine;

/// <summary>
/// Applies a conservative runtime profile for classroom demo hardware.
/// </summary>
[DisallowMultipleComponent]
public sealed class PerformanceProfile : MonoBehaviour
{
    private const string GraphicsPresetPrefKey = "SplatFighters.Menu.GraphicsPreset";

    private enum GraphicsPreset
    {
        Performant,
        Balanced,
        HighFidelity
    }

    [SerializeField, Min(15)] private int targetFrameRate = 30;
    [SerializeField] private bool disableVSync = true;
    [SerializeField, Min(0.01f)] private float fixedDeltaTime = 0.02f;
    [SerializeField] private bool applyOnAwake = true;

    private int previousTargetFrameRate;
    private int previousVSyncCount;
    private float previousFixedDeltaTime;
    private bool applied;

    public int TargetFrameRate => targetFrameRate;
    public bool DisableVSync => disableVSync;
    public float FixedDeltaTime => fixedDeltaTime;
    public bool IsApplied => applied;

    private void Awake()
    {
        LoadPresetFromPreferences();

        if (applyOnAwake)
        {
            Apply();
        }
    }

    private void OnDestroy()
    {
        Restore();
    }

    public void Apply()
    {
        if (applied)
        {
            return;
        }

        previousTargetFrameRate = Application.targetFrameRate;
        previousVSyncCount = QualitySettings.vSyncCount;
        previousFixedDeltaTime = Time.fixedDeltaTime;

        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        Application.targetFrameRate = targetFrameRate;
        Time.fixedDeltaTime = fixedDeltaTime;
        applied = true;
    }

    public void ApplySettings(int newTargetFrameRate, bool newDisableVSync, float newFixedDeltaTime)
    {
        Restore();
        targetFrameRate = Mathf.Max(15, newTargetFrameRate);
        disableVSync = newDisableVSync;
        fixedDeltaTime = Mathf.Max(0.01f, newFixedDeltaTime);
        Apply();
    }

    public void Restore()
    {
        if (!applied)
        {
            return;
        }

        Application.targetFrameRate = previousTargetFrameRate;
        QualitySettings.vSyncCount = previousVSyncCount;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        applied = false;
    }

    private void LoadPresetFromPreferences()
    {
        int rawPreset = PlayerPrefs.GetInt(GraphicsPresetPrefKey, (int)GraphicsPreset.Performant);

        switch ((GraphicsPreset)Mathf.Clamp(rawPreset, (int)GraphicsPreset.Performant, (int)GraphicsPreset.HighFidelity))
        {
            case GraphicsPreset.Performant:
                targetFrameRate = 30;
                disableVSync = true;
                fixedDeltaTime = 0.03f;
                break;
            case GraphicsPreset.Balanced:
                targetFrameRate = 45;
                disableVSync = true;
                fixedDeltaTime = 0.02f;
                break;
            case GraphicsPreset.HighFidelity:
                targetFrameRate = 60;
                disableVSync = false;
                fixedDeltaTime = 0.0166667f;
                break;
        }
    }
}
