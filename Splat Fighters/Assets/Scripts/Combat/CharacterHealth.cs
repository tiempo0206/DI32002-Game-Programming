using System;
using UnityEngine;

/// <summary>
/// Lightweight health model for the Turf War MVP.
/// Enemy-owned paint creates pressure by draining health and triggering a timed respawn.
/// </summary>
[DisallowMultipleComponent]
public class CharacterHealth : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private Team team = Team.TeamA;

    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float enemyPaintDamagePerSecond = 35f;
    [SerializeField] private bool damageOnlyDuringMatch = true;
    [SerializeField] private Transform groundProbe = null;

    [Header("Elimination State")]
    [SerializeField] private bool hideRenderersWhileEliminated = true;

    private CharacterController characterController;
    private PlayerController playerController;
    private BotController botController;
    private InkWeapon[] weapons = new InkWeapon[0];
    private Renderer[] renderers = new Renderer[0];
    private float currentHealth;
    private bool isEliminated;

    public event Action<CharacterHealth> Eliminated;

    public Team Team => team;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth * 100f;
    public bool IsEliminated => isEliminated;

    private void Awake()
    {
        ResolveReferences();
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        enemyPaintDamagePerSecond = Mathf.Max(0f, enemyPaintDamagePerSecond);
    }

    private void Update()
    {
        if (isEliminated || !CanTakeMatchDamage())
        {
            return;
        }

        if (TryGetEnemyPaintTeam(out Team enemyTeam))
        {
            ApplyDamage(enemyPaintDamagePerSecond * Time.deltaTime, enemyTeam);
        }
    }

    public void Configure(Team newTeam, Transform newGroundProbe)
    {
        team = newTeam;
        groundProbe = newGroundProbe;
    }

    public void ApplyDamage(float amount, Team sourceTeam)
    {
        if (amount <= 0f || isEliminated || sourceTeam == Team.None || sourceTeam == team)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            Eliminate();
        }
    }

    public void ReviveFull()
    {
        ResolveReferences();
        currentHealth = maxHealth;
        isEliminated = false;
        SetCharacterActive(true);
    }

    public void ResetHealth()
    {
        ReviveFull();
    }

    private void Eliminate()
    {
        if (isEliminated)
        {
            return;
        }

        isEliminated = true;
        currentHealth = 0f;
        SetCharacterActive(false);
        Eliminated?.Invoke(this);
    }

    private bool CanTakeMatchDamage()
    {
        if (!damageOnlyDuringMatch)
        {
            return true;
        }

        return GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.MatchState.Playing;
    }

    private bool TryGetEnemyPaintTeam(out Team enemyTeam)
    {
        enemyTeam = Team.None;

        if (PaintManager.Instance == null || team == Team.None)
        {
            return false;
        }

        Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position;

        if (!PaintManager.Instance.TryGetTeamAtWorldPosition(probePosition, out Team groundTeam))
        {
            return false;
        }

        if (groundTeam == Team.None || groundTeam == team)
        {
            return false;
        }

        enemyTeam = groundTeam;
        return true;
    }

    private void ResolveReferences()
    {
        if (groundProbe == null)
        {
            groundProbe = transform;
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (botController == null)
        {
            botController = GetComponent<BotController>();
        }

        weapons = GetComponentsInChildren<InkWeapon>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void SetCharacterActive(bool active)
    {
        if (characterController != null)
        {
            characterController.enabled = active;
        }

        if (playerController != null)
        {
            playerController.enabled = active;
        }

        if (botController != null)
        {
            botController.enabled = active;
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            InkWeapon weapon = weapons[i];

            if (weapon != null)
            {
                weapon.enabled = active;
            }
        }

        if (!hideRenderersWhileEliminated)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer characterRenderer = renderers[i];

            if (characterRenderer != null)
            {
                characterRenderer.enabled = active;
            }
        }
    }
}
