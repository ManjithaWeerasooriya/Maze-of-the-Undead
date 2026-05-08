using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAStar : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("A* Path Settings")]
    public float pathUpdateRate = 0.5f;
    public float waypointTolerance = 1.0f;

    [Header("Debug")]
    public bool showDebugPath = true;
    public float debugLogInterval = 1.0f;

    private NavMeshAgent agent;
    private List<Vector3> currentPath = new List<Vector3>();
    private int waypointIndex = 0;
    private float pathTimer = 0f;
    private float debugLogTimer = 0f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            Debug.LogError("[ZombieAStar] Player is not assigned.");
            enabled = false;
            return;
        }

        if (PathfindingGrid.Instance == null)
        {
            Debug.LogError("[ZombieAStar] PathfindingGrid is missing from the scene.");
            enabled = false;
            return;
        }

        CalculatePath();
    }

    private void Update()
    {
        pathTimer += Time.deltaTime;
        debugLogTimer += Time.deltaTime;

        if (pathTimer >= pathUpdateRate)
        {
            pathTimer = 0f;
            CalculatePath();
        }

        if (debugLogTimer >= debugLogInterval)
        {
            debugLogTimer = 0f;
            float distance = Vector3.Distance(transform.position, player.position);
            Debug.Log($"[ZombieAStar] Distance to player: {distance:F1}m | Waypoints: {currentPath?.Count ?? 0} | Current index: {waypointIndex}");
        }

        FollowPath();
    }

    private void CalculatePath()
    {
        if (player == null) return;

        List<Vector3> newPath = AStarPathfinder.FindPath(transform.position, player.position);

        if (newPath == null || newPath.Count == 0)
        {
            agent.SetDestination(player.position);
            return;
        }

        currentPath = newPath;
        waypointIndex = 0;
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        if (waypointIndex >= currentPath.Count)
            return;

        Vector3 targetWaypoint = currentPath[waypointIndex];

        float distToWaypoint = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetWaypoint.x, 0, targetWaypoint.z)
        );

        if (distToWaypoint <= waypointTolerance)
        {
            waypointIndex++;

            if (waypointIndex >= currentPath.Count)
                return;

            targetWaypoint = currentPath[waypointIndex];
        }

        agent.SetDestination(targetWaypoint);
    }

    private void OnEnable()
    {
        PathManager.OnPathChanged += CalculatePath;
    }

    private void OnDisable()
    {
        PathManager.OnPathChanged -= CalculatePath;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath || currentPath == null || currentPath.Count < 2)
            return;

        Gizmos.color = Color.red;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            Gizmos.DrawSphere(currentPath[i], 0.2f);
        }

        Gizmos.DrawSphere(currentPath[currentPath.Count - 1], 0.2f);
    }
}