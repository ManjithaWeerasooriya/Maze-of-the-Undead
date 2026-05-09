using System.Collections;
using UnityEngine;

/// <summary>
/// Plays ambient clips tied to AudioManager's SFX volume channel.
/// In RandomInterval mode: plays a random clip, waits a configurable silence gap, then picks another.
/// In Loop mode: loops a single randomly chosen clip continuously.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class AmbientSource : MonoBehaviour
{
    public enum PlaybackMode
    {
        /// <summary>Pick a random clip, play it fully, pause for a random interval, repeat.</summary>
        RandomInterval,
        /// <summary>Pick one random clip on enable and loop it continuously.</summary>
        Loop,
    }

    [Header("Clips")]
    [Tooltip("Assign multiple clips — one is chosen at random each time.")]
    [SerializeField] private AudioClip[] clips;
    [Tooltip("Resources/<path> fallback pool, e.g. 'SFX/Ambience/Wind'.")]
    [SerializeField] private string[] resourceNames;

    [Header("Playback")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.RandomInterval;
    [Tooltip("Minimum silence gap (seconds) between clips. Only used in RandomInterval mode.")]
    [SerializeField, Min(0f)] private float minInterval = 3f;
    [Tooltip("Maximum silence gap (seconds) between clips. Only used in RandomInterval mode.")]
    [SerializeField, Min(0f)] private float maxInterval = 8f;

    [Header("Volume")]
    [Tooltip("Final volume = baseVolume * Master * SFX.")]
    [Range(0f, 1f), SerializeField] private float baseVolume = 0.7f;

    private AudioSource _source;
    private AudioClip[] _pool;
    private Coroutine _playbackRoutine;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;

        _pool = BuildClipPool();
        if (_pool.Length == 0)
            Debug.LogWarning("[AmbientSource] No clips resolved — assign clips in the inspector or check resource names.", this);
    }

    private void OnEnable()
    {
        ApplyVolume();

        if (AudioManager.Instance != null)
            AudioManager.Instance.VolumeChanged.AddListener(OnVolumeChanged);

        if (_pool == null || _pool.Length == 0) return;

        if (playbackMode == PlaybackMode.Loop)
        {
            _source.loop = true;
            _source.clip = PickRandom();
            _source.Play();
        }
        else
        {
            _source.loop = false;
            _playbackRoutine = StartCoroutine(RandomIntervalRoutine());
        }
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.VolumeChanged.RemoveListener(OnVolumeChanged);

        if (_playbackRoutine != null)
        {
            StopCoroutine(_playbackRoutine);
            _playbackRoutine = null;
        }
        _source.Stop();
    }

    private IEnumerator RandomIntervalRoutine()
    {
        // Small random offset so multiple AmbientSources in the same scene don't sync up.
        yield return new WaitForSeconds(Random.Range(0f, minInterval));

        while (true)
        {
            _source.clip = PickRandom();
            _source.Play();

            // Wait for the clip to finish playing.
            yield return new WaitForSeconds(_source.clip.length);

            // Silence gap before the next sound.
            float gap = Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
            yield return new WaitForSeconds(gap);
        }
    }

    private void OnVolumeChanged(AudioManager.VolumeSnapshot _) => ApplyVolume();

    private void ApplyVolume()
    {
        float sfxScale = AudioManager.Instance != null ? AudioManager.Instance.EffectiveSfxVolume : 1f;
        _source.volume = baseVolume * sfxScale;
    }

    private AudioClip PickRandom() => _pool[Random.Range(0, _pool.Length)];

    private AudioClip[] BuildClipPool()
    {
        var pool = new System.Collections.Generic.List<AudioClip>();

        if (clips != null)
            foreach (var c in clips)
                if (c != null) pool.Add(c);

        if (resourceNames != null)
            foreach (var name in resourceNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var loaded = Resources.Load<AudioClip>(name);
                if (loaded != null) pool.Add(loaded);
                else Debug.LogWarning($"[AmbientSource] Resources/'{name}' not found.", this);
            }

        return pool.ToArray();
    }
}
