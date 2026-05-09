using UnityEngine;
using UnityEditor;

public class SpawnPointGizmos : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (gameObject.name.StartsWith("PlayerSpawn"))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Handles.color = Color.green;
            Handles.Label(transform.position + Vector3.up * 0.7f, "Player Spawn");
        }

        if (gameObject.name.StartsWith("ZombieSpawn"))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Handles.color = Color.red;
            Handles.Label(transform.position + Vector3.up * 0.7f, "Zombie Spawn");
        }
    }
}
