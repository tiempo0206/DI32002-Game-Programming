using UnityEngine;

/// <summary>
/// A reusable pickup that temporarily boosts the player and then respawns after a short delay.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class OverdrivePickup : MonoBehaviour
{
    [SerializeField] private float overdriveDuration = 7f;
    [SerializeField] private float respawnDelay = 15f;
    [SerializeField] private int healingAmount = 1;
    [SerializeField] private float bobHeight = 0.3f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float spinSpeed = 55f;
    [SerializeField] private AudioClip pickupSound = null;
    [SerializeField] [Range(0f, 1f)] private float pickupSoundVolume = 0.75f;

    private SpriteRenderer spriteRenderer = null;
    private Collider2D triggerCollider = null;
    private Vector3 startPosition = Vector3.zero;
    private float respawnAt = 0f;
    private bool collected = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        startPosition = transform.position;

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (collected)
        {
            if (Time.time >= respawnAt)
            {
                SetCollectedState(false);
            }

            return;
        }

        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + new Vector3(0f, bobOffset, 0f);
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected)
        {
            return;
        }

        PlayerOverdrive overdrive = collision.GetComponent<PlayerOverdrive>();
        if (overdrive == null)
        {
            overdrive = collision.GetComponentInParent<PlayerOverdrive>();
        }

        if (overdrive == null)
        {
            return;
        }

        overdrive.ApplyOverdrive(overdriveDuration);

        Health playerHealth = overdrive.GetComponent<Health>();
        if (playerHealth != null && healingAmount > 0)
        {
            playerHealth.ReceiveHealing(healingAmount);
        }

        if (pickupSound != null)
        {
            Vector3 soundPosition = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(pickupSound, soundPosition, pickupSoundVolume);
        }

        SetCollectedState(true);
    }

    private void SetCollectedState(bool isCollected)
    {
        collected = isCollected;
        transform.position = startPosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !isCollected;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = !isCollected;
        }

        if (isCollected)
        {
            respawnAt = Time.time + respawnDelay;
        }
    }
}
