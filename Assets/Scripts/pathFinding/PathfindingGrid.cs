using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance;
    [SerializeField] private float dynamicBlockPadding = 0.5f;

    private Dictionary<GameObject, List<PathNode>> dynamicBlocksByOwner = new Dictionary<GameObject, List<PathNode>>();

    public Vector2 gridWorldSize = new Vector2(300f, 300f);
    public float nodeRadius = 0.5f;
    public float navMeshSampleRadius = 5f;

    [Header("Waypoint Height Offset")]
    [SerializeField] private float waypointYOffset = 0.35f;


    [Header("Wall Detection (no layer setup needed)")]
    [Tooltip("Nodes within this distance of a NavMesh edge are marked blocked. " +
             "Increase if zombie still clips walls. 0.5-1.5 is usually good.")]
    public float wallEdgeBuffer = 1.0f;

    private PathNode[,] grid;
    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeZ;

    private void Awake()
    {
        Instance = this;
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }

    private void CreateGrid()
    {
        grid = new PathNode[gridSizeX, gridSizeZ];
        int walkableCount = 0;

        Vector3 bottomLeft = transform.position
            - Vector3.right   * gridWorldSize.x / 2f
            - Vector3.forward * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Vector3 worldPoint = bottomLeft
                    + Vector3.right   * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (z * nodeDiameter + nodeRadius);

                // Step 1: Is this point on the NavMesh?
                bool onNavMesh = NavMesh.SamplePosition(
                    worldPoint, out NavMeshHit hit,
                    navMeshSampleRadius, NavMesh.AllAreas);

                bool isWalkable = false;

                if (onNavMesh)
                {
                    // Step 2: Is this node too close to a NavMesh edge (wall boundary)?
                    // NavMesh.FindClosestEdge finds the nearest edge of the walkable area.
                    // If the edge is within wallEdgeBuffer units, the node is too close
                    // to a wall and we mark it blocked — this prevents paths hugging or
                    // cutting through walls entirely without needing any layer setup.
                    NavMeshHit edgeHit;
                    bool foundEdge = NavMesh.FindClosestEdge(hit.position, out edgeHit, NavMesh.AllAreas);

                    if (foundEdge)
                        isWalkable = edgeHit.distance >= wallEdgeBuffer;
                    else
                        isWalkable = true;
                }

                if (isWalkable) walkableCount++;

                Vector3 finalPos = onNavMesh ? hit.position + Vector3.up * waypointYOffset : worldPoint;
                grid[x, z] = new PathNode(x, z, isWalkable, finalPos);
            }
        }

        Debug.Log($"[PathfindingGrid] Grid built — Walkable: {walkableCount}/{gridSizeX * gridSizeZ} " +
                  $"({(walkableCount * 100f / (gridSizeX * gridSizeZ)):F1}%)");
    }

    public bool IsInsideGrid(Vector3 worldPosition)
    {
        float halfX = gridWorldSize.x / 2f;
        float halfZ = gridWorldSize.y / 2f;
        return worldPosition.x >= transform.position.x - halfX &&
               worldPosition.x <= transform.position.x + halfX &&
               worldPosition.z >= transform.position.z - halfZ &&
               worldPosition.z <= transform.position.z + halfZ;
    }

    public PathNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x / 2f) / gridWorldSize.x;
        float percentZ = (worldPosition.z - transform.position.z + gridWorldSize.y / 2f) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);

        return grid[x, z];
    }

    public PathNode NearestWalkableNode(Vector3 worldPosition, int searchRadius = 5)
    {
        PathNode origin = NodeFromWorldPoint(worldPosition);

        if (origin.walkable) return origin;

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;

                    int nx = origin.gridX + dx;
                    int nz = origin.gridZ + dz;

                    if (nx >= 0 && nx < gridSizeX && nz >= 0 && nz < gridSizeZ)
                    {
                        PathNode candidate = grid[nx, nz];
                        if (candidate.walkable) return candidate;
                    }
                }
            }
        }

        return origin;
    }

    public List<PathNode> GetNeighbours(PathNode node)
    {
        List<PathNode> neighbours = new List<PathNode>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;

                int checkX = node.gridX + dx;
                int checkZ = node.gridZ + dz;

                if (checkX < 0 || checkX >= gridSizeX || checkZ < 0 || checkZ >= gridSizeZ)
                    continue;

                PathNode neighbour = grid[checkX, checkZ];

                if (neighbour == null || !neighbour.walkable)
                    continue;
                

                // Block diagonals that cut through wall corners
                if (dx != 0 && dz != 0)
                {
                    bool cardinalX = grid[node.gridX + dx, node.gridZ].walkable;
                    bool cardinalZ = grid[node.gridX, node.gridZ + dz].walkable;

                    if (!cardinalX || !cardinalZ)
                        continue;
                }

                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    public void ResetAllNodes()
    {
        foreach (PathNode node in grid)
            node.ResetNode();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (grid == null)
        {
            return;
        }

        int step = Mathf.Max(1, gridSizeX / 40);

        for (int x = 0; x < gridSizeX; x += step)
        {
            for (int z = 0; z < gridSizeZ; z += step)
            {
                PathNode node = grid[x, z];

                if (node == null)
                {
                    continue;
                }

                if (node.dynamicallyBlocked)
                {
                    Gizmos.color = Color.yellow;
                }
                else if (node.walkable)
                {
                    Gizmos.color = new Color(0, 1, 0, 0.2f);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0.15f);
                }

                Gizmos.DrawCube(
                    node.worldPosition,
                    Vector3.one * (nodeDiameter * step * 0.8f)
                );
            }
        }
    }
    public int UpdateDynamicObstacle(GameObject owner, Bounds obstacleBounds)
    {
        if (owner == null)
        {
            Debug.LogWarning("[PathfindingGrid] Cannot update dynamic obstacle because owner is null.");
            return 0;
        }

        ClearDynamicBlocksForOwner(owner);

        List<PathNode> affectedNodes = GetNodesInBounds(obstacleBounds);

        foreach (PathNode node in affectedNodes)
        {
            if (node != null && node.baseWalkable)
            {
                node.dynamicallyBlocked = true;
            }
        }

        dynamicBlocksByOwner[owner] = affectedNodes;

        Debug.Log($"[PathfindingGrid] Blocked {affectedNodes.Count} dynamic nodes for {owner.name}.");

        return affectedNodes.Count;
    }
    public int ClearDynamicBlocksForOwner(GameObject owner)
    {
        if (owner == null)
        {
            return 0;
        }

        if (!dynamicBlocksByOwner.ContainsKey(owner))
        {
            return 0;
        }

        List<PathNode> oldNodes = dynamicBlocksByOwner[owner];

        foreach (PathNode node in oldNodes)
        {
            if (node != null)
            {
                node.dynamicallyBlocked = false;
            }
        }

        dynamicBlocksByOwner.Remove(owner);

        Debug.Log($"[PathfindingGrid] Cleared {oldNodes.Count} old dynamic nodes for {owner.name}.");

        return oldNodes.Count;
    }
    public List<PathNode> GetNodesInBounds(Bounds bounds)
    {
        List<PathNode> nodes = new List<PathNode>();

        if (grid == null)
        {
            Debug.LogWarning("[PathfindingGrid] Grid is null. Cannot detect nodes in bounds.");
            return nodes;
        }

        bounds.Expand(dynamicBlockPadding);

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                PathNode node = grid[x, z];

                if (node == null)
                {
                    continue;
                }

                Vector3 nodePosition = node.worldPosition;

                bool insideX = nodePosition.x >= bounds.min.x && nodePosition.x <= bounds.max.x;
                bool insideZ = nodePosition.z >= bounds.min.z && nodePosition.z <= bounds.max.z;

                if (insideX && insideZ)
                {
                    nodes.Add(node);
                }
            }
        }

        return nodes;
    }

}