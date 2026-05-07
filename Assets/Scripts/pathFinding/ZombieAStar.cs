using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAStar : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Path Update")]
    public float pathUpdateRate = 0.2f;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("Debug")]
    public bool showDebugPath = true;

    private NavMeshAgent agent;
    private Animator animator;
    private List<Vector3> currentPath = new List<Vector3>();

    private float timer;
    private float lastAttackTime;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

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

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            ChasePlayer();
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;

        timer += Time.deltaTime;

        if (timer >= pathUpdateRate)
        {
            timer = 0f;
            UpdatePathToPlayer();
        }
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;

        Vector3 lookDirection = player.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            Debug.Log("Zombie attacks player!");
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

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