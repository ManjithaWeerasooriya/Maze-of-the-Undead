using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SpawnSceneSetup
{
    [MenuItem("Tools/Setup Spawn Scene")]
    public static void SetupSpawnScene()
    {
        // Create SpawnManager GameObject
        GameObject spawnManagerObj = new GameObject("SpawnManager");
        spawnManagerObj.transform.position = Vector3.zero;
        SpawnManager spawnManager = spawnManagerObj.AddComponent<SpawnManager>();

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
        spawnManager = spawnManagerObj.GetComponent<SpawnManager>();
        spawnManager.playerSpawnPoints = playerSpawns;
        spawnManager.zombieSpawnPoints = zombieSpawns;

        // Load and assign prefabs
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player-v1.prefab");
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie/ZombieMale_AAB_URP.prefab");

        if (playerPrefab != null)
            spawnManager.playerPrefab = playerPrefab;
        else
            Debug.LogWarning("Player prefab not found at Assets/Prefabs/Player/Player-v1.prefab");

        if (zombiePrefab != null)
            spawnManager.zombiePrefab = zombiePrefab;
        else
            Debug.LogWarning("Zombie prefab not found at Assets/Prefabs/Zombie/ZombieMale_AAB_URP.prefab");

        // Set spawn configuration
        spawnManager.zombieSpawnInterval = 3f;
        spawnManager.maxZombies = 20;

        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(spawnManagerObj.scene);
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("Spawn scene setup complete!");
    }
}
