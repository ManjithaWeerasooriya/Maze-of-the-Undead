using UnityEditor;
using UnityEngine;

public class SpawnSceneSetup
{
    [MenuItem("Tools/Setup Spawn Scene")]
    public static void SetupSpawnScene()
    {
        // Create SpawnManager GameObject
        GameObject spawnManagerObj = new GameObject("SpawnManager");
        spawnManagerObj.transform.position = Vector3.zero;
        spawnManagerObj.AddComponent<SpawnManager>();

        // Create PlayerSpawn points
        Vector3[] playerSpawnPositions = new Vector3[]
        {
            new Vector3(-10, 1, -10),
            new Vector3(10, 1, -10),
            new Vector3(-10, 1, 10),
            new Vector3(10, 1, 10)
        };

        Transform[] playerSpawns = new Transform[playerSpawnPositions.Length];
        for (int i = 0; i < playerSpawnPositions.Length; i++)
        {
            GameObject playerSpawnObj = new GameObject("PlayerSpawn_" + i);
            playerSpawnObj.transform.position = playerSpawnPositions[i];
            playerSpawnObj.transform.parent = spawnManagerObj.transform;
            playerSpawns[i] = playerSpawnObj.transform;
        }

        // Create ZombieSpawn points
        Vector3[] zombieSpawnPositions = new Vector3[]
        {
            new Vector3(-15, 1, 0),
            new Vector3(15, 1, 0),
            new Vector3(0, 1, -15),
            new Vector3(0, 1, 15),
            new Vector3(-8, 1, 8),
            new Vector3(8, 1, -8)
        };

        Transform[] zombieSpawns = new Transform[zombieSpawnPositions.Length];
        for (int i = 0; i < zombieSpawnPositions.Length; i++)
        {
            GameObject zombieSpawnObj = new GameObject("ZombieSpawn_" + i);
            zombieSpawnObj.transform.position = zombieSpawnPositions[i];
            zombieSpawnObj.transform.parent = spawnManagerObj.transform;
            zombieSpawns[i] = zombieSpawnObj.transform;
        }

        // Assign spawn points to SpawnManager
        SpawnManager spawnManager = spawnManagerObj.GetComponent<SpawnManager>();
        spawnManager.playerSpawnPoints = playerSpawns;
        spawnManager.zombieSpawnPoints = zombieSpawns;

        Debug.Log("Spawn scene setup complete!");
    }
}
