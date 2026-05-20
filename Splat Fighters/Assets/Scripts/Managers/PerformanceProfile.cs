using UnityEngine;

/// <summary>
/// Applies a conservative runtime profile for classroom demo hardware.
/// </summary>
[DisallowMultipleComponent]
public sealed class PerformanceProfile : MonoBehaviour
{
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

    private void Awake()
    {
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
}
