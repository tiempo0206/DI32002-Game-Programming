using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current overdrive status in the HUD.
/// </summary>
public class PowerUpStatusDisplay : UIelement
{
    [SerializeField] private TextMeshProUGUI displayText = null;
    [SerializeField] private PlayerOverdrive targetOverdrive = null;
    [SerializeField] private Color readyColor = new Color(0.72f, 0.82f, 1f, 1f);
    [SerializeField] private Color activeColor = new Color(0.35f, 0.95f, 1f, 1f);

    private void Update()
    {
        RefreshDisplay();
    }

    public override void UpdateUI()
    {
        base.UpdateUI();
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (displayText == null)
        {
            return;
        }

        if (targetOverdrive == null && GameManager.instance != null && GameManager.instance.player != null)
        {
            targetOverdrive = GameManager.instance.player.GetComponent<PlayerOverdrive>();
        }

        if (targetOverdrive == null)
        {
            displayText.text = string.Empty;
            return;
        }

        if (targetOverdrive.IsActive)
        {
            displayText.color = activeColor;
            displayText.text = "OVERDRIVE: " + targetOverdrive.RemainingDuration.ToString("0.0") + "s";
        }
        else
        {
            displayText.color = readyColor;
            displayText.text = "OVERDRIVE READY";
        }
    }
}
