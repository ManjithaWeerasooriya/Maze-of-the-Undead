using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] public GameObject playerPrefab;
    [SerializeField] public GameObject zombiePrefab;
    
    [SerializeField] public Transform[] playerSpawnPoints;
    [SerializeField] public Transform[] zombieSpawnPoints;
    
    [SerializeField] public float zombieSpawnInterval = 3f;
    [SerializeField] public int maxZombies = 20;

    private int zombieCount = 0;
    private GameObject playerInstance;

    private void Start()
    {
        SpawnPlayer();
        InvokeRepeating(nameof(SpawnZombie), zombieSpawnInterval, zombieSpawnInterval);
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned!");
            return;
        }

        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
        {
            Debug.LogError("No player spawn points assigned!");
            return;
        }

        Transform spawnPoint = playerSpawnPoints[Random.Range(0, playerSpawnPoints.Length)];
        playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private void SpawnZombie()
    {
        if (zombieCount >= maxZombies)
            return;

        if (zombiePrefab == null)
        {
            Debug.LogError("Zombie prefab is not assigned!");
            return;
        }

        if (zombieSpawnPoints == null || zombieSpawnPoints.Length == 0)
        {
            Debug.LogError("No zombie spawn points assigned!");
            return;
        }

        Transform spawnPoint = null;
        int attempts = 0;
        const int maxAttempts = 10;
        const float minDistanceFromPlayer = 5f;

        while (attempts < maxAttempts)
        {
            spawnPoint = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Length)];
            
            if (playerInstance != null)
            {
                float distanceToPlayer = Vector3.Distance(spawnPoint.position, playerInstance.transform.position);
                if (distanceToPlayer >= minDistanceFromPlayer)
                {
                    break;
                }
            }
            else
            {
                break;
            }

            attempts++;
        }

        if (spawnPoint == null || (playerInstance != null && 
            Vector3.Distance(spawnPoint.position, playerInstance.transform.position) < minDistanceFromPlayer && 
            attempts >= maxAttempts))
        {
            Debug.LogWarning("Could not find safe spawn point for zombie after " + maxAttempts + " attempts. Skipping spawn.");
            return;
        }

        Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        zombieCount++;
    }

    public void OnZombieDied()
    {
        zombieCount--;
    }

    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnZombie));
    }

    public void RestartSpawning()
    {
        InvokeRepeating(nameof(SpawnZombie), zombieSpawnInterval, zombieSpawnInterval);
    }
}
