using UnityEngine;

/// <summary>
/// Drop one of these into a scene to tell <see cref="AudioManager"/> which music
/// track to cross-fade into when this scene loads. Keeps scene-specific music
/// data inside the scene rather than in a centralized lookup table.
/// </summary>
[DisallowMultipleComponent]
public class SceneMusic : MonoBehaviour
{
    [Tooltip("Direct clip assignment — takes priority over 'musicResourceName' if set.")]
    [SerializeField] private AudioClip musicClip;

    [Tooltip("Resources/<path> to the clip, e.g. 'Music/Main_Menu' or 'Music/Level_Exploration'. Used if no clip is assigned above.")]
    [SerializeField] private string musicResourceName;

    [Tooltip("Cross-fade duration in seconds (negative = use AudioManager's default).")]
    [SerializeField] private float fadeDuration = -1f;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneMusic] No AudioManager in scene — make sure it's loaded first (DontDestroyOnLoad).", this);
            return;
        }

        if (musicClip != null)
            AudioManager.Instance.PlayMusic(musicClip, fadeDuration);
        else if (!string.IsNullOrWhiteSpace(musicResourceName))
            AudioManager.Instance.PlayMusic(musicResourceName, fadeDuration);
        else
            Debug.LogWarning("[SceneMusic] No clip or resource name configured.", this);
    }
}
