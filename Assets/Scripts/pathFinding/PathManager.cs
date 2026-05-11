using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;
    public static event System.Action OnPathChanged;

    private void Awake()
    {
        Instance = this;
    }

    public void RecalculatePath()
    {
        OnPathChanged?.Invoke();
    }
    public void NotifyObstacleChanged(GameObject obstacle, Bounds obstacleBounds)
    {
        if (obstacle == null)
        {
            Debug.LogWarning("[PathManager] Obstacle changed event received, but obstacle is null.");
            return;
        }

        if (PathfindingGrid.Instance == null)
        {
            Debug.LogWarning("[PathManager] Cannot update graph because PathfindingGrid.Instance is missing.");
            return;
        }

        int blockedNodeCount = PathfindingGrid.Instance.UpdateDynamicObstacle(obstacle, obstacleBounds);

        Debug.Log($"[PathManager] Graph updated for {obstacle.name}. Blocked nodes: {blockedNodeCount}. Broadcasting OnPathChanged.");

        OnPathChanged?.Invoke();
    }
}