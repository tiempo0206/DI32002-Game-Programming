using System;
using UnityEngine;

/// <summary>
/// Small reusable countdown timer for MVP match flow.
/// It is not a MonoBehaviour so GameManager can own the update timing.
/// </summary>
[Serializable]
public class MatchTimer
{
    [SerializeField, Min(1f)] private float durationSeconds = 180f;

    private float remainingSeconds;
    private bool isRunning;

    public float DurationSeconds => durationSeconds;
    public float RemainingSeconds => remainingSeconds;
    public float ElapsedSeconds => Mathf.Max(0f, durationSeconds - remainingSeconds);
    public float NormalizedRemaining => durationSeconds <= 0f ? 0f : Mathf.Clamp01(remainingSeconds / durationSeconds);
    public bool IsRunning => isRunning;
    public bool IsFinished => remainingSeconds <= 0f;

    public void Configure(float newDurationSeconds)
    {
        durationSeconds = Mathf.Max(1f, newDurationSeconds);
        remainingSeconds = Mathf.Min(remainingSeconds, durationSeconds);
    }

    public void Reset()
    {
        remainingSeconds = durationSeconds;
        isRunning = false;
    }

    public void Start()
    {
        if (remainingSeconds <= 0f)
        {
            remainingSeconds = durationSeconds;
        }

        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;
    }

    public bool Tick(float deltaTime)
    {
        if (!isRunning)
        {
            return false;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Mathf.Max(0f, deltaTime));

        if (remainingSeconds > 0f)
        {
            return false;
        }

        isRunning = false;
        return true;
    }
}
