using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detects when the player enters the goal-room exit volume and raises events.
/// Does not handle the win screen itself — that is the GameManager's job.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ExitTrigger : MonoBehaviour
{
    public enum DetectionMode { ByTag, ByLayer }

    [Header("Player Detection")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.ByTag;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer = 1 << 6; // matches "Player" layer in TagManager.asset

    [Header("Behaviour")]
    [Tooltip("If true, the trigger fires once and is then disabled until ResetTrigger() is called.")]
    [SerializeField] private bool oneShot = true;
    [Tooltip("Minimum seconds between repeat triggers when oneShot is false.")]
    [SerializeField, Min(0f)] private float retriggerCooldown = 1f;

    [Header("Events")]
    [Tooltip("Inspector-friendly hook (UI sounds, juice, etc.).")]
    public UnityEvent OnPlayerExitReached;

    /// <summary>Static event so a GameManager (or anything else) can listen without a direct reference.</summary>
    public static event System.Action<ExitTrigger> ExitReached;

    private bool _hasTriggered;
    private float _cooldownTimer;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[ExitTrigger] Collider on '{name}' was not set as Trigger. Forcing isTrigger = true.", this);
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (oneShot && _hasTriggered) return;
        if (_cooldownTimer > 0f) return;

        _hasTriggered = true;
        _cooldownTimer = retriggerCooldown;

        OnPlayerExitReached?.Invoke();
        ExitReached?.Invoke(this);
    }

    private bool IsPlayer(Collider other)
    {
        if (detectionMode == DetectionMode.ByLayer)
            return ((1 << other.gameObject.layer) & playerLayer.value) != 0;

        if (other.CompareTag(playerTag)) return true;
        // Player rigidbodies often live on a parent — check the attached body's tag too.
        var rb = other.attachedRigidbody;
        return rb != null && rb.CompareTag(playerTag);
    }

    /// <summary>Re-arms the trigger so it can fire again. Useful for level reloads.</summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
        _cooldownTimer = 0f;
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Color fill = new Color(0.1f, 1f, 0.4f, 0.18f);
        Color outline = new Color(0.1f, 1f, 0.4f, 1f);
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        switch (col)
        {
            case BoxCollider box:
                Gizmos.color = fill;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = outline;
                Gizmos.DrawWireCube(box.center, box.size);
                break;
            case SphereCollider sph:
                Gizmos.color = fill;
                Gizmos.DrawSphere(sph.center, sph.radius);
                Gizmos.color = outline;
                Gizmos.DrawWireSphere(sph.center, sph.radius);
                break;
            case CapsuleCollider cap:
                Gizmos.color = outline;
                Gizmos.DrawWireSphere(cap.center, cap.radius);
                break;
        }

        Gizmos.matrix = prev;
    }
}
