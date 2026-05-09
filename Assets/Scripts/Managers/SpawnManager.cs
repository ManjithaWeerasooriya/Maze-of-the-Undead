using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] public GameObject playerPrefab;
    [SerializeField] public GameObject zombiePrefab;
    
    [SerializeField] public Transform[] playerSpawnPoints;
    [SerializeField] public Transform[] zombieSpawnPoints;
    
    [SerializeField] public float zombieSpawnInterval = 3f;
    [SerializeField] public int maxZombies = 20;
    [SerializeField] public int currentWave = 1;
    [SerializeField] public int zombiesPerWave = 3;
    [SerializeField] public int zombiesPerWaveIncrement = 2;

    private int zombieCount = 0;
    private int _zombiesRemainingInWave = 0;
    private GameObject playerInstance;

    private void Start()
    {
        SpawnPlayer();
        StartWave();
    }

    private void StartWave()
    {
        _zombiesRemainingInWave = zombiesPerWave + (currentWave - 1) * zombiesPerWaveIncrement;
        Debug.Log("Wave " + currentWave + " started. Zombies to spawn: " + _zombiesRemainingInWave);
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
        if (_zombiesRemainingInWave <= 0)
            return;

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
        _zombiesRemainingInWave--;
    }

    public void OnZombieDied()
    {
        zombieCount--;

        // Check if all zombies from current wave are dead
        if (zombieCount == 0 && _zombiesRemainingInWave <= 0)
        {
            Debug.Log("Wave " + currentWave + " complete!");
            CancelInvoke(nameof(SpawnZombie));
            currentWave++;
            StartWave();
        }
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
