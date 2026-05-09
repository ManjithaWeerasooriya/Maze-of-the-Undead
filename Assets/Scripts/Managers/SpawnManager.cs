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
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
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

        Transform spawnPoint = zombieSpawnPoints[Random.Range(0, zombieSpawnPoints.Length)];
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
