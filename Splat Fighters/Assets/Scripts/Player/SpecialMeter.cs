using UnityEngine;

/// <summary>
/// Charges a simple player special meter from territory painting.
/// </summary>
[DisallowMultipleComponent]
public class SpecialMeter : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private Team team = Team.TeamA;

    [Header("Charge")]
    [SerializeField, Min(1)] private int changedCellsForFullCharge = 180;
    [SerializeField, Range(0f, 100f)] private float startingChargePercent = 0f;
    [SerializeField] private bool resetWhenPaintCleared = true;

    private PaintManager subscribedPaintManager;
    private float currentChargeCells;

    public Team Team => team;
    public float ChargePercent => Mathf.Clamp01(currentChargeCells / changedCellsForFullCharge) * 100f;
    public bool IsReady => currentChargeCells >= changedCellsForFullCharge;

    private void Awake()
    {
        ResetChargeToStartingValue();
    }

    private void OnEnable()
    {
        TrySubscribeToPaintManager();
    }

    private void Start()
    {
        TrySubscribeToPaintManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromPaintManager();
    }

    public void ResetCharge()
    {
        currentChargeCells = 0f;
    }

    public void ResetChargeToStartingValue()
    {
        currentChargeCells = Mathf.Clamp01(startingChargePercent / 100f) * changedCellsForFullCharge;
    }

    public void AddChargeFromPaint(int changedCells)
    {
        if (changedCells <= 0 || IsReady)
        {
            return;
        }

        currentChargeCells = Mathf.Min(changedCellsForFullCharge, currentChargeCells + changedCells);
    }

    public bool ConsumeReadyCharge()
    {
        if (!IsReady)
        {
            return false;
        }

        ResetCharge();
        return true;
    }

    private void TrySubscribeToPaintManager()
    {
        PaintManager paintManager = PaintManager.Instance;

        if (paintManager == null || subscribedPaintManager == paintManager)
        {
            return;
        }

        UnsubscribeFromPaintManager();
        subscribedPaintManager = paintManager;
        subscribedPaintManager.PaintApplied += HandlePaintApplied;
        subscribedPaintManager.PaintCleared += HandlePaintCleared;
    }

    private void UnsubscribeFromPaintManager()
    {
        if (subscribedPaintManager == null)
        {
            return;
        }

        subscribedPaintManager.PaintApplied -= HandlePaintApplied;
        subscribedPaintManager.PaintCleared -= HandlePaintCleared;
        subscribedPaintManager = null;
    }

    private void HandlePaintApplied(Team paintTeam, int changedCells, Vector3 worldPosition, float radius)
    {
        if (paintTeam != team)
        {
            return;
        }

        AddChargeFromPaint(changedCells);
    }

    private void HandlePaintCleared()
    {
        if (!resetWhenPaintCleared)
        {
            return;
        }

        ResetCharge();
    }
}
