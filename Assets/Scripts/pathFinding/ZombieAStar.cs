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

    [Header("Attack Settings")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.5f;
    public float damage = 10f; // 🔥 ADDED
    public float rotationSpeed = 8f;

    [Header("Animation")]
    public Animator animator;
    public string walkBoolName = "isWalking";
    public string attackTriggerName = "Attack";

    [Header("Debug")]
    public bool showDebugPath = true;
    public float debugLogInterval = 1.0f;

    private NavMeshAgent agent;
    private List<Vector3> currentPath = new List<Vector3>();
    private int waypointIndex = 0;
    private float pathTimer = 0f;
    private float debugLogTimer = 0f;
    private float nextAttackTime = 0f;

    private PlayerHealth playerHealth;
    private ZombieSound zombieSound;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (player == null)
        {
            Debug.LogError("[ZombieAStar] Player is not assigned.");
            enabled = false;
            return;
        }

        zombieSound = GetComponent<ZombieSound>();

        // 🔥 CACHE PLAYER HEALTH HERE
        playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[ZombieAStar] PlayerHealth NOT found on player!");
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
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
            return;
        }

        agent.isStopped = false;

        if (animator != null)
        {
            animator.SetBool(walkBoolName, agent.velocity.magnitude > 0.1f);
        }

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
            Debug.Log($"[ZombieAStar] Distance: {distanceToPlayer:F1}m");
        }

        FollowPath();
    }

    private void AttackPlayer()
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (animator != null)
        {
            animator.SetBool(walkBoolName, false);
        }

        FacePlayer();

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;

            if (animator != null)
            {
                animator.SetTrigger(attackTriggerName);
            }

            Debug.Log("[ZombieAStar] Zombie attacks player!");
            zombieSound?.PlayAttack();
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                Debug.Log("🔥 DAMAGE APPLIED");
                playerHealth.TakeDamage(damage);
            }
            else
            {
                Debug.Log("❌ PlayerHealth NOT FOUND on player!");
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
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
        PathManager.OnPathChanged += HandlePathChanged;
    }
    private void HandlePathChanged()
{
    Debug.Log("[ZombieAStar] Path recalculated after graph change.");
    CalculatePath();
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