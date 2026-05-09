using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Central audio owner for music, ambience, and (future) SFX.
/// Persists across scene loads, exposes volume channels (Master / Music / SFX) that
/// the Settings panel can drive, and fades music on scene transitions and game-over.
///
/// The AudioManager has no dependency on the Settings panel. The Settings panel
/// should call the public Set*Volume methods and (optionally) subscribe to
/// <see cref="VolumeChanged"/> to reflect the current values back into its sliders.
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // PlayerPrefs keys are kept here so Settings code never needs to know them.
    private const string PrefMaster = "audio.master";
    private const string PrefMusic = "audio.music";
    private const string PrefSfx = "audio.sfx";
    private const string PrefMusicMuted = "audio.music.muted";

    [Header("Music Source")]
    [Tooltip("Clip played on game start. Optional — overridden by 'defaultMusicResource' if null.")]
    [SerializeField] private AudioClip defaultMusicClip;
    [Tooltip("Optional Resources/<name> fallback if no clip is assigned in the inspector.")]
    [SerializeField] private string defaultMusicResource = "Music/Theme";
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = true;

    [Header("Initial Volumes (used the first time the game runs)")]
    [Range(0f, 1f), SerializeField] private float initialMasterVolume = 1f;
    [Range(0f, 1f), SerializeField] private float initialMusicVolume = 0.7f;
    [Range(0f, 1f), SerializeField] private float initialSfxVolume = 1f;

    [Header("Fades")]
    [Tooltip("Seconds for fade in / fade out on PlayMusic / StopMusic.")]
    [SerializeField, Min(0f)] private float defaultFadeDuration = 1.0f;
    [Tooltip("Seconds for the cross-fade between two tracks (e.g. on scene change).")]
    [SerializeField, Min(0f)] private float crossFadeDuration = 1.5f;

    [Header("Behaviour")]
    [Tooltip("If true, music keeps playing while Time.timeScale = 0 (e.g. pause menu).")]
    [SerializeField] private bool musicIgnoresGamePause = true;
    [Tooltip("If true, music fades out automatically when the game state becomes Lost or Won.")]
    [SerializeField] private bool fadeMusicOnGameEnd = true;
    [Tooltip("Verbose logs to help wiring up the system.")]
    [SerializeField] private bool debugLogs = true;

    // Public state — Settings panel reads these to populate its sliders.
    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public bool IsMusicMuted { get; private set; }

    /// <summary>Final multiplier applied to the music source: Master * Music * (muted ? 0 : 1).</summary>
    public float EffectiveMusicVolume => IsMusicMuted ? 0f : MasterVolume * MusicVolume;

    /// <summary>Final multiplier any SFX caller should use: Master * SFX.</summary>
    public float EffectiveSfxVolume => MasterVolume * SfxVolume;

    /// <summary>
    /// Broadcast every time any volume value changes. Settings UI can listen
    /// to keep its sliders in sync if volumes are changed from elsewhere.
    /// </summary>
    public UnityEvent<VolumeSnapshot> VolumeChanged = new UnityEvent<VolumeSnapshot>();

    public struct VolumeSnapshot
    {
        public float master;
        public float music;
        public float sfx;
        public bool musicMuted;
    }

    private AudioSource _musicSource;
    private AudioClip _currentMusicClip;
    private Coroutine _activeFade;

    // ----------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = loopMusic;
        _musicSource.spatialBlend = 0f; // 2D
        _musicSource.ignoreListenerPause = musicIgnoresGamePause;

        LoadVolumesFromPrefs();
        ApplyMusicVolume();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= HandleGameStateChanged;
    }

    private void Start()
    {
        if (!playMusicOnStart) return;

        AudioClip clip = defaultMusicClip != null
            ? defaultMusicClip
            : LoadFromResources(defaultMusicResource);

        if (clip != null)
            PlayMusic(clip, defaultFadeDuration);
        else
            Debug.LogWarning($"[AudioManager] No music clip assigned and Resources/'{defaultMusicResource}' not found.", this);
    }

    // ----------------------------------------------------------------- volume API

    /// <summary>Settings panel hook — call this from a 0..1 master-volume slider.</summary>
    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefMaster, MasterVolume);
        ApplyMusicVolume();
        BroadcastVolumeChanged();
    }

    /// <summary>Settings panel hook — call this from a 0..1 music-volume slider.</summary>
    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefMusic, MusicVolume);
        ApplyMusicVolume();
        BroadcastVolumeChanged();
    }

    /// <summary>
    /// Settings panel hook — call this from a 0..1 SFX-volume slider.
    /// AudioManager stores the value; SFX-playing code reads <see cref="EffectiveSfxVolume"/>.
    /// </summary>
    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(PrefSfx, SfxVolume);
        BroadcastVolumeChanged();
    }

    /// <summary>Settings panel hook — bind to a "Mute Music" toggle.</summary>
    public void SetMusicMuted(bool muted)
    {
        IsMusicMuted = muted;
        PlayerPrefs.SetInt(PrefMusicMuted, muted ? 1 : 0);
        ApplyMusicVolume();
        BroadcastVolumeChanged();
    }

    /// <summary>Resets all volumes to the inspector-configured defaults.</summary>
    public void ResetVolumesToDefaults()
    {
        MasterVolume = initialMasterVolume;
        MusicVolume = initialMusicVolume;
        SfxVolume = initialSfxVolume;
        IsMusicMuted = false;
        PlayerPrefs.SetFloat(PrefMaster, MasterVolume);
        PlayerPrefs.SetFloat(PrefMusic, MusicVolume);
        PlayerPrefs.SetFloat(PrefSfx, SfxVolume);
        PlayerPrefs.SetInt(PrefMusicMuted, 0);
        ApplyMusicVolume();
        BroadcastVolumeChanged();
    }

    // ----------------------------------------------------------------- music API

    /// <summary>Cross-fade into a new music clip. Pass null/0 fadeDuration for an instant swap.</summary>
    public void PlayMusic(AudioClip clip, float fadeDuration = -1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] PlayMusic called with null clip.", this);
            return;
        }
        if (clip == _currentMusicClip && _musicSource.isPlaying) return;

        float fade = fadeDuration < 0f ? defaultFadeDuration : fadeDuration;
        StartFade(FadeToClip(clip, fade));
    }

    /// <summary>Plays a clip loaded from the Resources folder by name (e.g. "Music/Tension").</summary>
    public void PlayMusic(string resourceName, float fadeDuration = -1f)
    {
        var clip = LoadFromResources(resourceName);
        if (clip != null) PlayMusic(clip, fadeDuration);
    }

    public void PauseMusic()
    {
        if (_musicSource.isPlaying) _musicSource.Pause();
        if (debugLogs) Debug.Log("[AudioManager] Music paused.");
    }

    public void ResumeMusic()
    {
        if (!_musicSource.isPlaying) _musicSource.UnPause();
        if (debugLogs) Debug.Log("[AudioManager] Music resumed.");
    }

    /// <summary>Fades out and stops music. Use fadeDuration=0 to stop instantly.</summary>
    public void StopMusic(float fadeDuration = -1f)
    {
        float fade = fadeDuration < 0f ? defaultFadeDuration : fadeDuration;
        StartFade(FadeOutAndStop(fade));
    }

    // ----------------------------------------------------------------- internals

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Lazy-bind to GameManager — it may have been re-created in the new scene.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleGameStateChanged;
            GameManager.Instance.StateChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (!fadeMusicOnGameEnd) return;
        if (state == GameManager.GameState.Lost || state == GameManager.GameState.Won)
            StopMusic(crossFadeDuration);
    }

    private void LoadVolumesFromPrefs()
    {
        MasterVolume = PlayerPrefs.GetFloat(PrefMaster, initialMasterVolume);
        MusicVolume = PlayerPrefs.GetFloat(PrefMusic, initialMusicVolume);
        SfxVolume = PlayerPrefs.GetFloat(PrefSfx, initialSfxVolume);
        IsMusicMuted = PlayerPrefs.GetInt(PrefMusicMuted, 0) == 1;
    }

    private void ApplyMusicVolume()
    {
        if (_musicSource != null) _musicSource.volume = EffectiveMusicVolume;
    }

    private void BroadcastVolumeChanged()
    {
        VolumeChanged?.Invoke(new VolumeSnapshot
        {
            master = MasterVolume,
            music = MusicVolume,
            sfx = SfxVolume,
            musicMuted = IsMusicMuted,
        });
    }

    private AudioClip LoadFromResources(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName)) return null;
        var clip = Resources.Load<AudioClip>(resourceName);
        if (clip == null && debugLogs)
            Debug.LogWarning($"[AudioManager] Resources/'{resourceName}' not found.", this);
        return clip;
    }

    private void StartFade(IEnumerator routine)
    {
        if (_activeFade != null) StopCoroutine(_activeFade);
        _activeFade = StartCoroutine(routine);
    }

    private IEnumerator FadeToClip(AudioClip newClip, float duration)
    {
        // Fade out current track if one is playing.
        if (_musicSource.isPlaying && duration > 0f)
            yield return FadeMusicVolume(_musicSource.volume, 0f, duration * 0.5f);

        _musicSource.clip = newClip;
        _currentMusicClip = newClip;
        _musicSource.loop = loopMusic;
        _musicSource.volume = 0f;
        _musicSource.Play();

        if (debugLogs) Debug.Log($"[AudioManager] Now playing music: {newClip.name}");

        float fadeIn = duration > 0f ? duration * 0.5f : 0f;
        yield return FadeMusicVolume(0f, EffectiveMusicVolume, fadeIn);
        _activeFade = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        if (!_musicSource.isPlaying) yield break;
        yield return FadeMusicVolume(_musicSource.volume, 0f, duration);
        _musicSource.Stop();
        _currentMusicClip = null;
        _activeFade = null;
        if (debugLogs) Debug.Log("[AudioManager] Music stopped.");
    }

    /// <summary>Linear interpolation in unscaled time — works while Time.timeScale = 0.</summary>
    private IEnumerator FadeMusicVolume(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            _musicSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _musicSource.volume = to;
    }
}
