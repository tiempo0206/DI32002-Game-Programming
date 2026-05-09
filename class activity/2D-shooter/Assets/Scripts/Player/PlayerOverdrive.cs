using UnityEngine;

/// <summary>
/// Applies a temporary combat boost to the player after picking up an overdrive item.
/// </summary>
public class PlayerOverdrive : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Controller playerController = null;
    [SerializeField] private ShootingController[] shootingControllers = new ShootingController[0];
    [SerializeField] private SpriteRenderer[] tintedRenderers = new SpriteRenderer[0];

    [Header("Boost Settings")]
    [SerializeField] private float speedMultiplier = 1.4f;
    [SerializeField] private float fireRateMultiplier = 0.65f;
    [SerializeField] private Color overdriveTint = new Color(0.65f, 0.9f, 1f, 1f);

    private float baseMoveSpeed = 0f;
    private float[] baseFireRates = new float[0];
    private Color[] baseColors = new Color[0];
    private float remainingDuration = 0f;

    public bool IsActive
    {
        get
        {
            return remainingDuration > 0f;
        }
    }

    public float RemainingDuration
    {
        get
        {
            return Mathf.Max(0f, remainingDuration);
        }
    }

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<Controller>();
        }

        if (shootingControllers.Length == 0)
        {
            shootingControllers = GetComponentsInChildren<ShootingController>(true);
        }

        if (tintedRenderers.Length == 0)
        {
            tintedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        CacheBaseValues();
        ApplyVisualState(false);
    }

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        remainingDuration -= Time.deltaTime;

        if (remainingDuration <= 0f)
        {
            remainingDuration = 0f;
            RestoreBaseValues();
            GameManager.UpdateUIElements();
        }
    }

    public void ApplyOverdrive(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (playerController == null)
        {
            return;
        }

        remainingDuration = Mathf.Max(remainingDuration, duration);
        playerController.moveSpeed = baseMoveSpeed * speedMultiplier;

        for (int i = 0; i < shootingControllers.Length; i++)
        {
            if (shootingControllers[i] != null)
            {
                shootingControllers[i].fireRate = baseFireRates[i] * fireRateMultiplier;
            }
        }

        ApplyVisualState(true);
        GameManager.UpdateUIElements();
    }

    private void CacheBaseValues()
    {
        if (playerController != null)
        {
            baseMoveSpeed = playerController.moveSpeed;
        }

        baseFireRates = new float[shootingControllers.Length];
        for (int i = 0; i < shootingControllers.Length; i++)
        {
            baseFireRates[i] = shootingControllers[i] != null ? shootingControllers[i].fireRate : 0f;
        }

        baseColors = new Color[tintedRenderers.Length];
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            baseColors[i] = tintedRenderers[i] != null ? tintedRenderers[i].color : Color.white;
        }
    }

    private void RestoreBaseValues()
    {
        if (playerController != null)
        {
            playerController.moveSpeed = baseMoveSpeed;
        }

        for (int i = 0; i < shootingControllers.Length; i++)
        {
            if (shootingControllers[i] != null)
            {
                shootingControllers[i].fireRate = baseFireRates[i];
            }
        }

        ApplyVisualState(false);
    }

    private void ApplyVisualState(bool active)
    {
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            if (tintedRenderers[i] == null)
            {
                continue;
            }

            tintedRenderers[i].color = active ? overdriveTint : baseColors[i];
        }
    }
}
