using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAI : MonoBehaviour
{
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float runSpeed = 3.5f;
    [SerializeField] int maxHealth = 100;
    [SerializeField] LayerMask playerLayer;

    enum State { Idle, Chase, Attack, Dead }

    NavMeshAgent agent;
    Animator animator;
    Transform player;
    State state = State.Idle;
    int health;

    static readonly int SpeedParam = Animator.StringToHash("Speed");
    static readonly int IsAttackingParam = Animator.StringToHash("IsAttacking");
    static readonly int IsDeadParam = Animator.StringToHash("IsDead");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = maxHealth;
    }

    void OnEnable() => PathManager.OnPathChanged += OnPathChanged;
    void OnDisable() => PathManager.OnPathChanged -= OnPathChanged;

    void Update()
    {
        if (state == State.Dead) return;

        DetectPlayer();

        switch (state)
        {
            case State.Idle:
                animator.SetFloat(SpeedParam, 0f);
                break;

            case State.Chase:
                ChasePlayer();
                break;

            case State.Attack:
                AttackPlayer();
                break;
        }
    }

    void DetectPlayer()
    {
        if (state == State.Attack) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (hits.Length > 0)
        {
            player = hits[0].transform;
            float dist = Vector3.Distance(transform.position, player.position);
            state = dist <= attackRange ? State.Attack : State.Chase;
        }
        else if (state == State.Chase)
        {
            state = State.Idle;
            agent.ResetPath();
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            state = State.Attack;
            return;
        }

        agent.speed = dist > detectionRadius * 0.5f ? walkSpeed : runSpeed;
        agent.SetDestination(player.position);
        animator.SetFloat(SpeedParam, agent.velocity.magnitude);
    }

    void AttackPlayer()
    {
        agent.ResetPath();
        animator.SetFloat(SpeedParam, 0f);

        if (player != null)
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        animator.SetBool(IsAttackingParam, true);

        // Return to chase if player moved out of range
        if (player != null && Vector3.Distance(transform.position, player.position) > attackRange * 1.5f)
        {
            animator.SetBool(IsAttackingParam, false);
            state = State.Chase;
        }
    }

    public void TakeDamage(int damage)
    {
        if (state == State.Dead) return;

        health -= damage;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        state = State.Dead;
        agent.enabled = false;
        animator.SetBool(IsDeadParam, true);
        enabled = false;
    }

    void OnPathChanged()
    {
        if (state == State.Chase && player != null)
            agent.SetDestination(player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
