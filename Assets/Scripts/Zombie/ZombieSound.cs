using System.Collections;
using UnityEngine;

/// <summary>
/// Plays idle moans and attack sounds for a zombie.
/// Idle sounds run on an automatic random timer.
/// Attack sounds are triggered externally via <see cref="PlayAttack"/>.
/// No two sounds ever overlap — attack interrupts idle; idle never interrupts anything.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class ZombieSound : MonoBehaviour
{
    [Header("Idle Sounds")]
    [Tooltip("Assign zombie-moan clips directly, or use Idle Resource Names below.")]
    [SerializeField] private AudioClip[] idleClips;
    [Tooltip("Resources/<path> for each idle clip, e.g. 'Zombie/zombie-moan1'.")]
    [SerializeField] private string[] idleResourceNames;
    [Tooltip("Shortest silence between idle moans (seconds).")]
    [SerializeField, Min(0f)] private float minIdleInterval = 4f;
    [Tooltip("Longest silence between idle moans (seconds).")]
    [SerializeField, Min(0f)] private float maxIdleInterval = 10f;

    [Header("Attack Sounds")]
    [Tooltip("Assign ZombieAttack clips directly, or use Attack Resource Names below.")]
    [SerializeField] private AudioClip[] attackClips;
    [Tooltip("Resources/<path> for each attack clip, e.g. 'Zombie/ZombieAttack1'.")]
    [SerializeField] private string[] attackResourceNames;

    [Header("Volume")]
    [Range(0f, 1f), SerializeField] private float baseVolume = 1f;

    [Header("3D Audio")]
    [SerializeField, Min(0f)] private float minDistance = 2f;
    [SerializeField, Min(0f)] private float maxDistance = 20f;

    private AudioSource _source;
    private AudioClip[] _idlePool;
    private AudioClip[] _attackPool;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 1f;                          // fully 3D
        _source.rolloffMode = AudioRolloffMode.Linear;
        _source.minDistance = minDistance;
        _source.maxDistance = maxDistance;

        _idlePool = BuildPool(idleClips, idleResourceNames);
        _attackPool = BuildPool(attackClips, attackResourceNames);

        if (_idlePool.Length == 0)
            Debug.LogWarning("[ZombieSound] No idle clips resolved.", this);
        if (_attackPool.Length == 0)
            Debug.LogWarning("[ZombieSound] No attack clips resolved.", this);
    }

    private void OnEnable()
    {
        ApplyVolume();
        if (AudioManager.Instance != null)
            AudioManager.Instance.VolumeChanged.AddListener(OnVolumeChanged);

        if (_idlePool.Length > 0)
            StartCoroutine(IdleRoutine());
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.VolumeChanged.RemoveListener(OnVolumeChanged);

        StopAllCoroutines();
        _source.Stop();
    }

    // ----------------------------------------------------------------- public API

    /// <summary>
    /// Call this from ZombieAI (or an animation event) when the zombie attacks.
    /// Interrupts any idle moan currently playing.
    /// </summary>
    public void PlayAttack()
    {
        if (_attackPool.Length == 0) return;
        // Attack takes priority — stop whatever is playing.
        _source.Stop();
        PlayOneShot(_attackPool[Random.Range(0, _attackPool.Length)]);
    }

    // ----------------------------------------------------------------- idle loop

    private IEnumerator IdleRoutine()
    {
        // Stagger start so a group of zombies doesn't moan in unison.
        yield return new WaitForSeconds(Random.Range(0f, maxIdleInterval));

        while (true)
        {
            if (!_source.isPlaying)
            {
                var clip = _idlePool[Random.Range(0, _idlePool.Length)];
                PlayOneShot(clip);
                // Wait for this clip to finish before starting the silence gap.
                yield return new WaitForSeconds(clip.length);
            }

            float gap = Random.Range(minIdleInterval, Mathf.Max(minIdleInterval, maxIdleInterval));
            yield return new WaitForSeconds(gap);
        }
    }

    // ----------------------------------------------------------------- helpers

    private void PlayOneShot(AudioClip clip)
    {
        _source.clip = clip;
        _source.volume = EffectiveVolume();
        _source.Play();
    }

    private void OnVolumeChanged(AudioManager.VolumeSnapshot _) => ApplyVolume();

    private void ApplyVolume() => _source.volume = EffectiveVolume();

    private float EffectiveVolume()
    {
        float sfx = AudioManager.Instance != null ? AudioManager.Instance.EffectiveSfxVolume : 1f;
        return baseVolume * sfx;
    }

    private AudioClip[] BuildPool(AudioClip[] direct, string[] resourceNames)
    {
        var pool = new System.Collections.Generic.List<AudioClip>();

        if (direct != null)
            foreach (var c in direct)
                if (c != null) pool.Add(c);

        if (resourceNames != null)
            foreach (var name in resourceNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var loaded = Resources.Load<AudioClip>(name);
                if (loaded != null) pool.Add(loaded);
                else Debug.LogWarning($"[ZombieSound] Resources/'{name}' not found.", this);
            }

        return pool.ToArray();
    }
}
