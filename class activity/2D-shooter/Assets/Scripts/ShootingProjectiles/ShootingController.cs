using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A class which controls aiming and shooting.
/// </summary>
public class ShootingController : MonoBehaviour
{
    [Header("GameObject/Component References")]
    [Tooltip("The projectile to be fired.")]
    public GameObject projectilePrefab = null;

    [Tooltip("The transform in the hierarchy which holds projectiles if any")]
    public Transform projectileHolder = null;

    [Header("Input Settings, Actions, & Controls")]
    [Tooltip("Whether this shooting controller is controlled by the player")]
    public bool isPlayerControlled = false;

    public InputAction fireAction;

    [Header("Firing Settings")]
    [Tooltip("The minimum time between projectiles being fired.")]
    public float fireRate = 0.05f;

    [Tooltip("The maximum difference between the direction the shooting controller is facing and the direction projectiles are launched.")]
    public float projectileSpread = 1.0f;

    [Header("Effects")]
    [Tooltip("The effect to create when this fires")]
    public GameObject fireEffect = null;

    [Tooltip("The sound to play when this fires")]
    public AudioClip fireSound = null;

    [Tooltip("The volume used for the fire sound")]
    [Range(0f, 1f)] public float fireSoundVolume = 0.85f;

    private float lastFired = Mathf.NegativeInfinity;

    private void OnEnable()
    {
        fireAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
    }

    private void Update()
    {
        ProcessInput();
    }

    private void Start()
    {
        if (fireAction.bindings.Count == 0 && isPlayerControlled)
        {
            Debug.LogWarning("The Fire Input Action does not have a binding set but is set to be player controlled! Make sure that it has a binding or the shooting controller will not shoot!");
        }
    }

    private void ProcessInput()
    {
        if (!isPlayerControlled)
        {
            return;
        }

        if (fireAction.bindings.Count == 0)
        {
            Debug.LogError("The Fire Input Action does not have a binding set! It must have a binding set in order to fire!");
        }

        if (fireAction.ReadValue<float>() >= 1)
        {
            Fire();
        }
    }

    public void Fire()
    {
        if ((Time.timeSinceLevelLoad - lastFired) <= fireRate)
        {
            return;
        }

        SpawnProjectile();

        if (fireEffect != null)
        {
            Instantiate(fireEffect, transform.position, transform.rotation, null);
        }

        PlayFireSound();
        lastFired = Time.timeSinceLevelLoad;
    }

    public void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        GameObject projectileGameObject = Instantiate(projectilePrefab, transform.position, transform.rotation, null);

        Vector3 rotationEulerAngles = projectileGameObject.transform.rotation.eulerAngles;
        rotationEulerAngles.z += Random.Range(-projectileSpread, projectileSpread);
        projectileGameObject.transform.rotation = Quaternion.Euler(rotationEulerAngles);

        if (projectileHolder == null && GameObject.Find("ProjectileHolder") != null)
        {
            projectileHolder = GameObject.Find("ProjectileHolder").transform;
        }

        if (projectileHolder != null)
        {
            projectileGameObject.transform.SetParent(projectileHolder);
        }
    }

    private void PlayFireSound()
    {
        if (fireSound == null)
        {
            return;
        }

        Vector3 soundPosition = transform.position;
        if (Camera.main != null)
        {
            soundPosition = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(fireSound, soundPosition, fireSoundVolume);
    }
}
