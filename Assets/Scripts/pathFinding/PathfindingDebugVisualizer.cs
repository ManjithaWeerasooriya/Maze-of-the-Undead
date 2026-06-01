using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathfindingDebugVisualizer : MonoBehaviour
{
    [SerializeField] private bool debugModeEnabled = false;
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.V;
    
    [Header("Visualization Colors")]
    [SerializeField] private Color openSetColor = new Color(1f, 1f, 0f, 0.5f); // Yellow - frontier
    [SerializeField] private Color closedSetColor = new Color(1f, 0f, 0f, 0.5f); // Red - explored
    [SerializeField] private Color pathColor = new Color(0f, 1f, 0f, 1f); // Green - path
    [SerializeField] private Color startColor = new Color(0f, 0f, 1f, 1f); // Blue - start
    [SerializeField] private Color goalColor = new Color(1f, 0.5f, 0f, 1f); // Orange - goal

    [Header("Visualization Settings")]
    [SerializeField] private float nodeVisualizationSize = 0.3f;
    [SerializeField] private float pathLineWidth = 0.1f;
    [SerializeField] private float waypointSphereSize = 0.2f;

    [Header("Algorithm Selection")]
    [SerializeField] private SearchAlgorithm currentAlgorithm = SearchAlgorithm.AStar;

    private List<PathNode> lastOpenSet = new List<PathNode>();
    private List<PathNode> lastClosedSet = new List<PathNode>();
    private List<Vector3> lastPath = new List<Vector3>();
    private Vector3 lastStartPos = Vector3.zero;
    private Vector3 lastGoalPos = Vector3.zero;

    private Vector3 lastTestedStartPos = Vector3.zero;
    private Vector3 lastTestedGoalPos = Vector3.zero;

    public enum SearchAlgorithm
    {
        AStar,
        BFS,
        UCS
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleDebugKey))
        {
            debugModeEnabled = !debugModeEnabled;
            Debug.Log($"[PathfindingDebugVisualizer] Debug mode {(debugModeEnabled ? "ENABLED" : "DISABLED")}");
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugModeEnabled)
            return;

        // Draw last computed path and search frontier
        if (lastPath.Count > 0)
        {
            DrawPath(lastPath);
        }

        if (lastOpenSet.Count > 0)
        {
            DrawOpenSet(lastOpenSet);
        }

        if (lastClosedSet.Count > 0)
        {
            DrawClosedSet(lastClosedSet);
        }

        if (lastStartPos != Vector3.zero)
        {
            Gizmos.color = startColor;
            Gizmos.DrawSphere(lastStartPos, waypointSphereSize);
        }

        if (lastGoalPos != Vector3.zero)
        {
            Gizmos.color = goalColor;
            Gizmos.DrawSphere(lastGoalPos, waypointSphereSize);
        }

        // Display algorithm info
        DrawDebugInfo();
    }

    public void VisualizePathfinding(Vector3 startPos, Vector3 goalPos)
    {
        if (!debugModeEnabled)
            return;

        // Only recompute if positions changed
        if (Vector3.Distance(startPos, lastTestedStartPos) < 0.1f &&
            Vector3.Distance(goalPos, lastTestedGoalPos) < 0.1f)
            return;

        lastTestedStartPos = startPos;
        lastTestedGoalPos = goalPos;

        lastStartPos = startPos;
        lastGoalPos = goalPos;
        lastPath.Clear();
        lastOpenSet.Clear();
        lastClosedSet.Clear();

        PathfindingGrid grid = PathfindingGrid.Instance;
        if (grid == null)
            return;

        NavMeshHit startHit, goalHit;
        if (!NavMesh.SamplePosition(startPos, out startHit, 5f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(goalPos, out goalHit, 5f, NavMesh.AllAreas))
            return;

        PathNode startNode = grid.NearestWalkableNode(startHit.position);
        PathNode goalNode = grid.NearestWalkableNode(goalHit.position);

        if (startNode == null || goalNode == null)
            return;

        grid.ResetAllNodes();

        // Execute the chosen algorithm and capture frontier data
        List<Vector3> path = null;
        
        switch (currentAlgorithm)
        {
            case SearchAlgorithm.AStar:
                path = FindPathWithVisualization_AStar(startNode, goalNode, grid);
                break;
            case SearchAlgorithm.BFS:
                path = FindPathWithVisualization_BFS(startNode, goalNode, grid);
                break;
            case SearchAlgorithm.UCS:
                path = FindPathWithVisualization_UCS(startNode, goalNode, grid);
                break;
        }

        if (path != null && path.Count > 0)
        {
            lastPath = new List<Vector3>(path);
        }

        Debug.Log($"[PathfindingDebugVisualizer] Visualization complete: {currentAlgorithm}, " +
                  $"Path: {lastPath.Count} waypoints, " +
                  $"Explored: {lastClosedSet.Count}, " +
                  $"Frontier: {lastOpenSet.Count}");
    }

    private List<Vector3> FindPathWithVisualization_AStar(PathNode startNode, PathNode goalNode, PathfindingGrid grid)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, goalNode);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            int lowestIndex = 0;

            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < currentNode.fCost ||
                    openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost)
                {
                    currentNode = openList[i];
                    lowestIndex = i;
                }
            }

            openList.RemoveAt(lowestIndex);
            closedSet.Add(currentNode);

            if (currentNode == goalNode)
            {
                lastClosedSet = new List<PathNode>(closedSet);
                lastOpenSet = new List<PathNode>(openList);
                return RetracePath(startNode, goalNode);
            }

            foreach (PathNode neighbour in grid.GetNeighbours(currentNode))
            {
                if ((!neighbour.walkable && neighbour != goalNode) || closedSet.Contains(neighbour))
                    continue;

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);

                if (newCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, goalNode);
                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        lastClosedSet = new List<PathNode>(closedSet);
        lastOpenSet = new List<PathNode>(openList);
        return null;
    }

    private List<Vector3> FindPathWithVisualization_BFS(PathNode startNode, PathNode goalNode, PathfindingGrid grid)
    {
        Queue<PathNode> openQueue = new Queue<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        openQueue.Enqueue(startNode);

        while (openQueue.Count > 0)
        {
            PathNode currentNode = openQueue.Dequeue();
            closedSet.Add(currentNode);

            if (currentNode == goalNode)
            {
                lastClosedSet = new List<PathNode>(closedSet);
                lastOpenSet = new List<PathNode>(openQueue);
                return RetracePath(startNode, goalNode);
            }

            foreach (PathNode neighbour in grid.GetNeighbours(currentNode))
            {
                if ((!neighbour.walkable && neighbour != goalNode) || closedSet.Contains(neighbour))
                    continue;

                if (!closedSet.Contains(neighbour) && !openQueue.Contains(neighbour))
                {
                    neighbour.gCost = currentNode.gCost + 1;
                    neighbour.parent = currentNode;
                    openQueue.Enqueue(neighbour);
                }
            }
        }

        lastClosedSet = new List<PathNode>(closedSet);
        lastOpenSet = new List<PathNode>();
        return null;
    }

    private List<Vector3> FindPathWithVisualization_UCS(PathNode startNode, PathNode goalNode, PathfindingGrid grid)
    {
        List<PathNode> openList = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            int lowestIndex = 0;

            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].gCost < currentNode.gCost)
                {
                    currentNode = openList[i];
                    lowestIndex = i;
                }
            }

            openList.RemoveAt(lowestIndex);
            closedSet.Add(currentNode);

            if (currentNode == goalNode)
            {
                lastClosedSet = new List<PathNode>(closedSet);
                lastOpenSet = new List<PathNode>(openList);
                return RetracePath(startNode, goalNode);
            }

            foreach (PathNode neighbour in grid.GetNeighbours(currentNode))
            {
                if ((!neighbour.walkable && neighbour != goalNode) || closedSet.Contains(neighbour))
                    continue;

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);

                if (newCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        lastClosedSet = new List<PathNode>(closedSet);
        lastOpenSet = new List<PathNode>(openList);
        return null;
    }

    private List<Vector3> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private void DrawPath(List<Vector3> path)
    {
        if (path.Count < 2)
            return;

        Gizmos.color = pathColor;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
            Gizmos.DrawSphere(path[i], waypointSphereSize);
        }
        Gizmos.DrawSphere(path[path.Count - 1], waypointSphereSize);
    }

    private void DrawOpenSet(List<PathNode> openSet)
    {
        Gizmos.color = openSetColor;
        foreach (PathNode node in openSet)
        {
            Gizmos.DrawCube(node.worldPosition, Vector3.one * nodeVisualizationSize);
        }
    }

    private void DrawClosedSet(List<PathNode> closedSet)
    {
        Gizmos.color = closedSetColor;
        foreach (PathNode node in closedSet)
        {
            Gizmos.DrawCube(node.worldPosition, Vector3.one * nodeVisualizationSize);
        }
    }

    private void DrawDebugInfo()
    {
        // Debug info is shown in console logs
        // Could be extended with OnGUI for in-game UI display
    }

    public void CycleAlgorithm()
    {
        currentAlgorithm = (SearchAlgorithm)(((int)currentAlgorithm + 1) % 3);
        Debug.Log($"[PathfindingDebugVisualizer] Algorithm switched to: {currentAlgorithm}");
    }

    private int GetDistance(PathNode nodeA, PathNode nodeB)
    {
        int distanceX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distanceZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        if (distanceX > distanceZ)
            return 14 * distanceZ + 10 * (distanceX - distanceZ);

        return 14 * distanceX + 10 * (distanceZ - distanceX);
    }

    public bool IsDebugModeEnabled => debugModeEnabled;
    public SearchAlgorithm CurrentAlgorithm => currentAlgorithm;
}
