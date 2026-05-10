using UnityEngine;

public class BarricadeWatcher : MonoBehaviour
{
    [SerializeField] private float movementThreshold = 0.5f;
    [SerializeField] private float rotationThreshold = 5f;
    [SerializeField] private float notificationCooldown = 0.25f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastNotificationTime;
    private Collider obstacleCollider;

    private void Awake()
    {
        obstacleCollider = GetComponent<Collider>();

        if (obstacleCollider == null)
        {
            Debug.LogWarning($"[BarricadeWatcher] No collider found on {gameObject.name}.");
        }
    }

    private void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        NotifyObstacleChanged();
    }

    private void Update()
    {
        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        float rotatedAngle = Quaternion.Angle(transform.rotation, lastRotation);

        bool movedEnough = movedDistance > movementThreshold;
        bool rotatedEnough = rotatedAngle > rotationThreshold;
        bool cooldownPassed = Time.time - lastNotificationTime >= notificationCooldown;

        if ((movedEnough || rotatedEnough) && cooldownPassed)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastNotificationTime = Time.time;

            Debug.Log($"[DynamicObstacle] {gameObject.name} moved or rotated.");

            NotifyObstacleChanged();
        }
    }

    private void OnDisable()
    {
        ClearDynamicBlocks();
    }

    private void OnDestroy()
    {
        ClearDynamicBlocks();
    }

    private void NotifyObstacleChanged()
    {
        if (PathManager.Instance == null)
        {
            Debug.LogWarning("[BarricadeWatcher] PathManager.Instance is missing.");
            return;
        }

        if (obstacleCollider == null)
        {
            Debug.LogWarning($"[BarricadeWatcher] Cannot notify obstacle change because {gameObject.name} has no collider.");
            return;
        }

        PathManager.Instance.NotifyObstacleChanged(gameObject, obstacleCollider.bounds);
    }

    private void ClearDynamicBlocks()
    {
        if (PathfindingGrid.Instance == null)
        {
            return;
        }

        PathfindingGrid.Instance.ClearDynamicBlocksForOwner(gameObject);
    }
}