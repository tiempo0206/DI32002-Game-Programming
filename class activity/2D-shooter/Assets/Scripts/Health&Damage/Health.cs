using UnityEngine;

/// <summary>
/// This class handles the health state of a game object.
/// Implementation Notes: 2D Rigidbodies must be set to never sleep for this to interact with trigger stay damage.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Team Settings")]
    [Tooltip("The team associated with this damage")]
    public int teamId = 0;

    [Header("Health Settings")]
    [Tooltip("The default health value")]
    public int defaultHealth = 1;

    [Tooltip("The maximum health value")]
    public int maximumHealth = 1;

    [Tooltip("The current in game health value")]
    public int currentHealth = 1;

    [Tooltip("Invulnerability duration, in seconds, after taking damage")]
    public float invincibilityTime = 3f;

    [Tooltip("Whether or not this health is always invincible")]
    public bool isAlwaysInvincible = false;

    [Header("Lives settings")]
    [Tooltip("Whether or not to use lives")]
    public bool useLives = false;

    [Tooltip("Current number of lives this health has")]
    public int currentLives = 3;

    [Tooltip("The maximum number of lives this health can have")]
    public int maximumLives = 5;

    [Header("Effects & Polish")]
    [Tooltip("The effect to create when this health dies")]
    public GameObject deathEffect = null;

    [Tooltip("The effect to create when this health is damaged")]
    public GameObject hitEffect = null;

    [Tooltip("The sound to play when this object takes damage")]
    public AudioClip hitSound = null;

    [Tooltip("The sound to play when this object dies")]
    public AudioClip deathSound = null;

    [Tooltip("Volume used for this health's audio clips")]
    [Range(0f, 1f)] public float soundVolume = 1f;

    private float timeToBecomeDamagableAgain = 0f;
    private bool isInvincableFromDamage = false;
    private Vector3 respawnPosition = Vector3.zero;

    private void Start()
    {
        SetRespawnPoint(transform.position);
    }

    private void Update()
    {
        InvincibilityCheck();
    }

    private void InvincibilityCheck()
    {
        if (timeToBecomeDamagableAgain <= Time.time)
        {
            isInvincableFromDamage = false;
        }
    }

    public void SetRespawnPoint(Vector3 newRespawnPosition)
    {
        respawnPosition = newRespawnPosition;
    }

    private void Respawn()
    {
        transform.position = respawnPosition;
        currentHealth = defaultHealth;
        GameManager.UpdateUIElements();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isInvincableFromDamage || isAlwaysInvincible)
        {
            return;
        }

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation, null);
        }

        PlayClip(hitSound);
        timeToBecomeDamagableAgain = Time.time + invincibilityTime;
        isInvincableFromDamage = true;
        currentHealth -= damageAmount;
        GameManager.UpdateUIElements();
        CheckDeath();
    }

    public void ReceiveHealing(int healingAmount)
    {
        currentHealth += healingAmount;
        if (currentHealth > maximumHealth)
        {
            currentHealth = maximumHealth;
        }

        GameManager.UpdateUIElements();
        CheckDeath();
    }

    private bool CheckDeath()
    {
        if (currentHealth <= 0)
        {
            Die();
            return true;
        }

        return false;
    }

    public void Die()
    {
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation, null);
        }

        PlayClip(deathSound);

        if (useLives)
        {
            HandleDeathWithLives();
        }
        else
        {
            HandleDeathWithoutLives();
        }
    }

    private void HandleDeathWithLives()
    {
        currentLives -= 1;
        GameManager.UpdateUIElements();

        if (currentLives > 0)
        {
            Respawn();
            return;
        }

        if (gameObject.name == "Player" && gameObject.tag != "Player")
        {
            Debug.LogWarning("It looks like you're trying to kill a player, but your player hasn't been tagged as 'Player' in the inspector! \n Please tag your player.");
        }

        if (gameObject.tag == "Player" && GameManager.instance != null)
        {
            GameManager.instance.GameOver();
        }

        if (gameObject.GetComponent<Enemy>() != null)
        {
            gameObject.GetComponent<Enemy>().DoBeforeDestroy();
        }

        Destroy(gameObject);
        GameManager.UpdateUIElements();
    }

    private void HandleDeathWithoutLives()
    {
        if (gameObject.tag == "Player" && GameManager.instance != null)
        {
            GameManager.instance.GameOver();
        }

        if (gameObject.GetComponent<Enemy>() != null)
        {
            gameObject.GetComponent<Enemy>().DoBeforeDestroy();
        }

        Destroy(gameObject);
        GameManager.UpdateUIElements();
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        Vector3 soundPosition = transform.position;
        if (Camera.main != null)
        {
            soundPosition = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(clip, soundPosition, soundVolume);
    }
}
