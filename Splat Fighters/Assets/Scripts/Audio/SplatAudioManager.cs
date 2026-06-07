using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns global music, sound effects, and saved volume preferences for Splat Fighters.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-260)]
public sealed class SplatAudioManager : MonoBehaviour
{
    private const string AudioManagerPrefabResourcePath = "Audio/Prefabs/SplatAudioManager";
    private const string MasterVolumePrefKey = "SplatFighters.Audio.MasterVolume";
    private const string MusicVolumePrefKey = "SplatFighters.Audio.MusicVolume";
    private const string SfxVolumePrefKey = "SplatFighters.Audio.SfxVolume";
    private const float DefaultMasterVolume = 0.85f;
    private const float DefaultMusicVolume = 0.72f;
    private const float DefaultSfxVolume = 0.9f;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource = null;
    [SerializeField] private AudioSource sfxSource = null;

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic = null;
    [SerializeField] private AudioClip gameplayMusic = null;

    [Header("UI SFX")]
    [SerializeField] private AudioClip uiClickClip = null;
    [SerializeField] private AudioClip uiConfirmClip = null;
    [SerializeField] private AudioClip uiBackClip = null;
    [SerializeField] private AudioClip selectionMoveClip = null;

    [Header("Gameplay SFX")]
    [SerializeField] private AudioClip weaponFireClip = null;
    [SerializeField] private AudioClip inkImpactClip = null;
    [SerializeField] private AudioClip matchStartClip = null;
    [SerializeField] private AudioClip matchEndClip = null;
    [SerializeField] private AudioClip specialBurstClip = null;

    private float masterVolume = DefaultMasterVolume;
    private float musicVolume = DefaultMusicVolume;
    private float sfxVolume = DefaultSfxVolume;

    public static SplatAudioManager Instance { get; private set; }

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
        LoadDefaultClipsIfNeeded();
        LoadSavedVolumes();
        ApplyVolumes();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    public static float GetMasterVolume()
    {
        return Instance != null ? Instance.MasterVolume : LoadVolume(MasterVolumePrefKey, DefaultMasterVolume);
    }

    public static float GetMusicVolume()
    {
        return Instance != null ? Instance.MusicVolume : LoadVolume(MusicVolumePrefKey, DefaultMusicVolume);
    }

    public static float GetSfxVolume()
    {
        return Instance != null ? Instance.SfxVolume : LoadVolume(SfxVolumePrefKey, DefaultSfxVolume);
    }

    public static void SetMasterVolumeValue(float volume)
    {
        SplatAudioManager manager = EnsureInstance();

        if (manager != null)
        {
            manager.SetMasterVolume(volume);
        }
        else
        {
            SaveVolume(MasterVolumePrefKey, volume);
        }
    }

    public static void SetMusicVolumeValue(float volume)
    {
        SplatAudioManager manager = EnsureInstance();

        if (manager != null)
        {
            manager.SetMusicVolume(volume);
        }
        else
        {
            SaveVolume(MusicVolumePrefKey, volume);
        }
    }

    public static void SetSfxVolumeValue(float volume)
    {
        SplatAudioManager manager = EnsureInstance();

        if (manager != null)
        {
            manager.SetSfxVolume(volume);
        }
        else
        {
            SaveVolume(SfxVolumePrefKey, volume);
        }
    }

    public static void PlayUiClickSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.uiClickClip, 0.72f, 0.02f);
    }

    public static void PlayUiConfirmSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.uiConfirmClip, 0.9f, 0.01f);
    }

    public static void PlayUiBackSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.uiBackClip, 0.78f, 0.01f);
    }

    public static void PlaySelectionMoveSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.selectionMoveClip, 0.8f, 0.025f);
    }

    public static void PlayWeaponFireSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.weaponFireClip, 0.74f, 0.045f);
    }

    public static void PlayInkImpactSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.inkImpactClip, 0.82f, 0.035f);
    }

    public static void PlayMatchStartSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.matchStartClip, 0.92f, 0f);
    }

    public static void PlayMatchEndSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.matchEndClip, 0.9f, 0f);
    }

    public static void PlaySpecialBurstSound()
    {
        SplatAudioManager manager = EnsureInstance();
        manager?.PlaySfx(manager.specialBurstClip, 0.88f, 0.02f);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveVolume(MasterVolumePrefKey, masterVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        SaveVolume(MusicVolumePrefKey, musicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveVolume(SfxVolumePrefKey, sfxVolume);
        ApplyVolumes();
    }

    private static SplatAudioManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        SplatAudioManager existing = FindObjectOfType<SplatAudioManager>();

        if (existing != null)
        {
            return existing;
        }

        SplatAudioManager prefab = Resources.Load<SplatAudioManager>(AudioManagerPrefabResourcePath);

        if (prefab != null)
        {
            return Instantiate(prefab);
        }

        GameObject fallbackObject = new GameObject("SplatAudioManager");
        return fallbackObject.AddComponent<SplatAudioManager>();
    }

    private void EnsureSources()
    {
        if (musicSource == null)
        {
            musicSource = CreateAudioSource("MusicSource", true);
        }

        if (sfxSource == null)
        {
            sfxSource = CreateAudioSource("SfxSource", false);
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private AudioSource CreateAudioSource(string sourceName, bool loop)
    {
        Transform sourceTransform = transform.Find(sourceName);
        GameObject sourceObject = sourceTransform != null ? sourceTransform.gameObject : new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.GetComponent<AudioSource>();

        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.loop = loop;
        return source;
    }

    private void LoadDefaultClipsIfNeeded()
    {
        menuMusic = menuMusic != null ? menuMusic : Resources.Load<AudioClip>("Audio/Music/MenuLoop");
        gameplayMusic = gameplayMusic != null ? gameplayMusic : Resources.Load<AudioClip>("Audio/Music/GameplayLoop");
        uiClickClip = uiClickClip != null ? uiClickClip : Resources.Load<AudioClip>("Audio/SFX/UiClick");
        uiConfirmClip = uiConfirmClip != null ? uiConfirmClip : Resources.Load<AudioClip>("Audio/SFX/UiConfirm");
        uiBackClip = uiBackClip != null ? uiBackClip : Resources.Load<AudioClip>("Audio/SFX/UiBack");
        selectionMoveClip = selectionMoveClip != null ? selectionMoveClip : Resources.Load<AudioClip>("Audio/SFX/SelectionMove");
        weaponFireClip = weaponFireClip != null ? weaponFireClip : Resources.Load<AudioClip>("Audio/SFX/WeaponFire");
        inkImpactClip = inkImpactClip != null ? inkImpactClip : Resources.Load<AudioClip>("Audio/SFX/InkImpact");
        matchStartClip = matchStartClip != null ? matchStartClip : Resources.Load<AudioClip>("Audio/SFX/MatchStart");
        matchEndClip = matchEndClip != null ? matchEndClip : Resources.Load<AudioClip>("Audio/SFX/MatchEnd");
        specialBurstClip = specialBurstClip != null ? specialBurstClip : Resources.Load<AudioClip>("Audio/SFX/SpecialBurst");
    }

    private void LoadSavedVolumes()
    {
        masterVolume = LoadVolume(MasterVolumePrefKey, DefaultMasterVolume);
        musicVolume = LoadVolume(MusicVolumePrefKey, DefaultMusicVolume);
        sfxVolume = LoadVolume(SfxVolumePrefKey, DefaultSfxVolume);
    }

    private static float LoadVolume(string key, float fallback)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback));
    }

    private static void SaveVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = masterVolume * musicVolume;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip targetClip = IsGameplayScene(sceneName) ? gameplayMusic : menuMusic;
        PlayMusic(targetClip);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return sceneName == "MVP_ShootingTest"
            || sceneName == "HowToPlayTraining"
            || sceneName.Contains("Shooting")
            || sceneName.Contains("Arena");
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null)
        {
            return;
        }

        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void PlaySfx(AudioClip clip, float volumeScale, float pitchJitter)
    {
        if (sfxSource == null || clip == null || masterVolume <= 0f || sfxVolume <= 0f)
        {
            return;
        }

        sfxSource.pitch = pitchJitter > 0f ? 1f + Random.Range(-pitchJitter, pitchJitter) : 1f;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * sfxVolume * volumeScale));
    }
}
