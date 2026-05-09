using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current player health in the HUD.
/// </summary>
public class PlayerHealthDisplay : UIelement
{
    [Tooltip("The text UI to use for display")]
    public TextMeshProUGUI displayText = null;

    [Tooltip("Optional direct reference to the player health component")]
    public Health targetHealth = null;

    public override void UpdateUI()
    {
        base.UpdateUI();

        if (targetHealth == null && GameManager.instance != null && GameManager.instance.player != null)
        {
            targetHealth = GameManager.instance.player.GetComponent<Health>();
        }

        if (displayText == null || targetHealth == null)
        {
            return;
        }

        displayText.text = "HP: " + Mathf.Max(targetHealth.currentHealth, 0) + "/" + targetHealth.maximumHealth;
    }
}
