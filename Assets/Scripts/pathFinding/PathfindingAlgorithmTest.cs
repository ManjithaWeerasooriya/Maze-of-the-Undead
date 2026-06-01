using System.Collections.Generic;
using UnityEngine;

public class PathfindingAlgorithmTest : MonoBehaviour
{
    [SerializeField] private Vector3 testStartPosition = Vector3.zero;
    [SerializeField] private Vector3 testGoalPosition = new Vector3(50f, 0f, 50f);

    [SerializeField] private bool runAStarTest = true;
    [SerializeField] private bool runBFSTest = true;
    [SerializeField] private bool runUCSTest = true;

    [SerializeField] private bool enableDebugVisualizer = true;

    private PathfindingDebugVisualizer debugVisualizer;

    private void Start()
    {
        Debug.Log("[PathfindingAlgorithmTest] Test initialized. Press Play to run tests.");

        // Find or create debug visualizer
        debugVisualizer = FindObjectOfType<PathfindingDebugVisualizer>();
        if (debugVisualizer == null && enableDebugVisualizer)
        {
            GameObject debugVisualizerGO = new GameObject("PathfindingDebugVisualizer");
            debugVisualizer = debugVisualizerGO.AddComponent<PathfindingDebugVisualizer>();
            Debug.Log("[PathfindingAlgorithmTest] Created PathfindingDebugVisualizer");
        }

        RunTests();
    }

    private void RunTests()
    {
        Debug.Log("\n========== PATHFINDING ALGORITHM TESTS ==========\n");

        if (PathfindingGrid.Instance == null)
        {
            Debug.LogError("[PathfindingAlgorithmTest] PathfindingGrid not found in scene!");
            return;
        }

        if (runAStarTest)
        {
            Debug.Log("[TEST] Running A* Algorithm...");
            TestAlgorithm("A*", () => AStarPathfinder.FindPath(testStartPosition, testGoalPosition));
        }

        if (runBFSTest)
        {
            Debug.Log("[TEST] Running BFS Algorithm...");
            TestAlgorithm("BFS", () => BFSPathfinder.FindPath(testStartPosition, testGoalPosition));
        }

        if (runUCSTest)
        {
            Debug.Log("[TEST] Running UCS Algorithm...");
            TestAlgorithm("UCS", () => UCSPathfinder.FindPath(testStartPosition, testGoalPosition));
        }

        // Test debug visualizer
        if (enableDebugVisualizer && debugVisualizer != null)
        {
            Debug.Log("[TEST] Testing Debug Visualizer...");
            debugVisualizer.VisualizePathfinding(testStartPosition, testGoalPosition);
            Debug.Log($"[TEST] Debug visualizer ready. Press 'V' to toggle visualization.");
        }

        Debug.Log("\n========== TESTS COMPLETE ==========\n");
    }

    private void TestAlgorithm(string algorithmName, System.Func<List<Vector3>> pathFindingMethod)
    {
        long startTime = System.Diagnostics.Stopwatch.GetTimestamp();

        List<Vector3> path = pathFindingMethod();

        long endTime = System.Diagnostics.Stopwatch.GetTimestamp();
        double elapsedMs = (endTime - startTime) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        if (path != null && path.Count > 0)
        {
            Debug.Log($"✓ {algorithmName} SUCCESS: Found path with {path.Count} waypoints in {elapsedMs:F3}ms");
            Debug.Log($"  Start: {path[0]}, Goal: {path[path.Count - 1]}");
        }
        else
        {
            Debug.LogWarning($"✗ {algorithmName} FAILED: No path found (execution time: {elapsedMs:F3}ms)");
        }
    }

    private void Update()
    {
        // Allow dynamic testing
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[PathfindingAlgorithmTest] Rerunning tests...");
            RunTests();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (debugVisualizer != null)
            {
                debugVisualizer.CycleAlgorithm();
            }
        }
    }
}
