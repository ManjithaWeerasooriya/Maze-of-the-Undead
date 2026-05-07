using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAStar : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Path Update")]
    public float pathUpdateRate = 0.2f;

    [Header("Debug")]
    public bool showDebugPath = true;

    private NavMeshAgent agent;
    private List<Vector3> currentPath = new List<Vector3>();
    private float timer;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("Zombie needs NavMeshAgent component.");
            return;
        }

        if (player == null)
        {
            Debug.LogError("Player is not assigned.");
            return;
        }

        timer = pathUpdateRate;
    }

    private void Update()
    {
        if (player == null || agent == null)
            return;

        timer += Time.deltaTime;

        if (timer >= pathUpdateRate)
        {
            timer = 0f;
            UpdatePathToPlayer();
        }
    }

    private void UpdatePathToPlayer()
    {
        NavMeshHit zombieHit;
        NavMeshHit playerHit;

        bool zombieFound = NavMesh.SamplePosition(
            transform.position,
            out zombieHit,
            5f,
            NavMesh.AllAreas
        );

        bool playerFound = NavMesh.SamplePosition(
            player.position,
            out playerHit,
            5f,
            NavMesh.AllAreas
        );

        if (!zombieFound)
        {
            Debug.LogWarning("Zombie is not on or near NavMesh.");
            return;
        }

        if (!playerFound)
        {
            Debug.LogWarning("Player is not on or near NavMesh.");
            return;
        }

        NavMeshPath calculatedPath = new NavMeshPath();

        bool pathFound = NavMesh.CalculatePath(
            zombieHit.position,
            playerHit.position,
            NavMesh.AllAreas,
            calculatedPath
        );

        if (!pathFound || calculatedPath.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("No complete path found.");
            return;
        }

        currentPath.Clear();

        for (int i = 0; i < calculatedPath.corners.Length; i++)
        {
            currentPath.Add(calculatedPath.corners[i]);
        }

        agent.SetDestination(playerHit.position);

        Debug.Log("Path updated to moving player. Points: " + currentPath.Count);
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