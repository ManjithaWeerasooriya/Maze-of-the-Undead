using System.Collections.Generic;
using UnityEngine;

public class ZombieAI : MonoBehaviour
{
    public float speed = 6f;

    public List<Transform> path;
    private int index = 0;

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
}