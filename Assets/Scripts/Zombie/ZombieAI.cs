using System.Collections.Generic;
using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    public float speed = 6f;

    public List<Transform> path;
    private int index = 0;

    public float damage = 10f;
    public float attackRate = 1f;
    private float nextAttackTime = 0f;

    void Update()
    {
        FollowPath();
    }

    void FollowPath()
    {
        if (path == null || path.Count == 0) return;

        Transform target = path[index];

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            index++;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Zombie touching player!");

            if (Time.time >= nextAttackTime)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    Debug.Log("Damage applied!");
                    playerHealth.TakeDamage(damage);
                }
                  else
                {
                    Debug.Log("❌ PlayerHealth NOT FOUND on: " + other.name);
                }

                nextAttackTime = Time.time + attackRate;
            }
        }
    }
}