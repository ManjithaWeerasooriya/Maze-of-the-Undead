using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] float detectionRadius  = 10f;
    [SerializeField] float attackRange      = 1.5f;
    [SerializeField] LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] float chaseSpeed       = 3.5f;
    [SerializeField] float waypointTolerance = 0.8f;

    [Header("Pathfinding")]
    [Tooltip("Seconds between A* recalculations while chasing")]
    [SerializeField] float pathRefreshRate  = 0.2f;

    [Header("Health")]
    [SerializeField] int maxHealth          = 100;

    // ── State ────────────────────────────────────────────────────────────────
    enum State { Idle, Chase, Attack, Dead }
    State state = State.Idle;

    NavMeshAgent    agent;
    Animator        animator;
    Transform       player;
    Vector3         lastKnownPlayerPos;
    bool            playerInSight;

    List<Vector3>   currentPath   = new List<Vector3>();
    int             waypointIndex = 0;

    int health;
    Coroutine pathRoutine;

    // Animator parameter hashes
    static readonly int SpeedHash      = Animator.StringToHash("Speed");
    static readonly int AttackingHash  = Animator.StringToHash("IsAttacking");
    static readonly int DeadHash       = Animator.StringToHash("IsDead");

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        agent   = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health  = maxHealth;
    }

    void OnEnable()  => PathManager.OnPathChanged += OnPathChanged;
    void OnDisable() => PathManager.OnPathChanged -= OnPathChanged;

    void Update()
    {
        if (state == State.Dead) return;

        ScanForPlayer();

        switch (state)
        {
            case State.Idle:
                animator.SetFloat(SpeedHash, 0f);
                break;

            case State.Chase:
                FollowPath();
                break;

            case State.Attack:
                HandleAttack();
                break;
        }
    }

    // ── Detection ────────────────────────────────────────────────────────────

    void ScanForPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        playerInSight = hits.Length > 0;

        if (playerInSight)
        {
            player = hits[0].transform;
            lastKnownPlayerPos = player.position;

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= attackRange)
            {
                EnterAttack();
                return;
            }

            if (state == State.Idle)
                EnterChase();
        }
        else if (state == State.Attack)
        {
            // Player ran away from melee range — resume chasing
            animator.SetBool(AttackingHash, false);
            EnterChase();
        }
        else if (state == State.Idle)
        {
            // Nothing to do while idle
        }
        // If chasing and player left detection radius, we still pursue
        // the lastKnownPlayerPos (handled in FollowPath)
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    void EnterChase()
    {
        state = State.Chase;
        agent.speed = chaseSpeed;

        if (pathRoutine != null) StopCoroutine(pathRoutine);
        pathRoutine = StartCoroutine(PathRefreshLoop());
    }

    IEnumerator PathRefreshLoop()
    {
        while (state == State.Chase)
        {
            Vector3 target = playerInSight ? player.position : lastKnownPlayerPos;
            RequestPath(target);
            yield return new WaitForSeconds(pathRefreshRate);
        }
    }

    void RequestPath(Vector3 target)
    {
        // Use A* when the grid is available, otherwise fall back to NavMesh directly
        if (PathfindingGrid.Instance != null)
        {
            List<Vector3> path = AStarPathfinder.FindPath(transform.position, target);
            if (path != null && path.Count > 0)
            {
                currentPath   = path;
                waypointIndex = 0;
                return;
            }
        }

        // Fallback: single-waypoint direct NavMesh path
        currentPath   = new List<Vector3> { target };
        waypointIndex = 0;
    }

    void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            animator.SetFloat(SpeedHash, 0f);

            // If we've reached the last known position without finding the player, go idle
            if (!playerInSight)
            {
                state = State.Idle;
                if (pathRoutine != null) { StopCoroutine(pathRoutine); pathRoutine = null; }
                agent.ResetPath();
            }
            return;
        }

        // Advance waypoint index when close enough to current waypoint
        while (waypointIndex < currentPath.Count - 1 &&
               Vector3.Distance(transform.position, currentPath[waypointIndex]) < waypointTolerance)
        {
            waypointIndex++;
        }

        Vector3 nextWaypoint = currentPath[waypointIndex];

        // If we've arrived at the final waypoint
        if (waypointIndex == currentPath.Count - 1 &&
            Vector3.Distance(transform.position, nextWaypoint) < waypointTolerance)
        {
            if (playerInSight && Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                EnterAttack();
            }
            else if (!playerInSight)
            {
                // Reached last known position, no player found — go idle
                state = State.Idle;
                if (pathRoutine != null) { StopCoroutine(pathRoutine); pathRoutine = null; }
                currentPath.Clear();
                agent.ResetPath();
            }
            animator.SetFloat(SpeedHash, 0f);
            return;
        }

        agent.SetDestination(nextWaypoint);
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    // ── Attack ───────────────────────────────────────────────────────────────

    void EnterAttack()
    {
        if (state == State.Attack) return;
        state = State.Attack;
        if (pathRoutine != null) { StopCoroutine(pathRoutine); pathRoutine = null; }
        agent.ResetPath();
        currentPath.Clear();
        animator.SetBool(AttackingHash, true);
        animator.SetFloat(SpeedHash, 0f);
    }

    void HandleAttack()
    {
        if (player != null)
        {
            // Face the player while attacking
            Vector3 dir = (player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }

    // ── Damage / Death ───────────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (state == State.Dead) return;
        health -= damage;
        if (health <= 0) Die();
    }

    void Die()
    {
        state = State.Dead;
        if (pathRoutine != null) StopCoroutine(pathRoutine);
        agent.enabled = false;
        animator.SetBool(DeadHash, true);
        enabled = false;
    }

    // ── PathManager callback ─────────────────────────────────────────────────

    void OnPathChanged()
    {
        // Force an immediate A* recalculation when a door or barricade moves
        if (state == State.Chase)
        {
            Vector3 target = playerInSight ? player.position : lastKnownPlayerPos;
            RequestPath(target);
        }
    }

    // ── Debug gizmos ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentPath == null || currentPath.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < currentPath.Count - 1; i++)
            Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
    }
}
