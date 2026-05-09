using TMPro;
using UnityEngine;

/// <summary>
/// Displays the current enemy defeat objective progress.
/// </summary>
public class ObjectiveDisplay : UIelement
{
    [Tooltip("The text UI to use for display")]
    public TextMeshProUGUI displayText = null;

    public override void UpdateUI()
    {
        base.UpdateUI();

        if (displayText == null || GameManager.instance == null)
        {
            return;
        }

        displayText.text = "TARGET: " + GameManager.instance.EnemiesDefeated + "/" + GameManager.instance.EnemiesToDefeatCount;
    }
}
