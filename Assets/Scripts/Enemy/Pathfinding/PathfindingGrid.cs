using UnityEngine;
using UnityEngine.AI;

public class PathfindingGrid : MonoBehaviour
{
    public static PathfindingGrid Instance;

    [Tooltip("Width (X) and depth (Z) of the grid in world units")]
    [SerializeField] Vector2 gridWorldSize = new Vector2(50f, 50f);
    [Tooltip("Half-size of each node; smaller = more accurate but more nodes")]
    [SerializeField] float nodeRadius = 0.5f;

    PathNode[,] grid;
    float nodeDiameter;
    int sizeX, sizeZ;

    void Awake()
    {
        Instance = this;
        nodeDiameter = nodeRadius * 2f;
        sizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        sizeZ = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        BuildGrid();
    }

    void BuildGrid()
    {
        grid = new PathNode[sizeX, sizeZ];
        Vector3 origin = transform.position
            - Vector3.right   * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                Vector3 worldPos = origin
                    + Vector3.right   * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (z * nodeDiameter + nodeRadius);

                // A node is walkable if the NavMesh covers it
                bool walkable = NavMesh.SamplePosition(worldPos, out _, nodeRadius * 2f, NavMesh.AllAreas);
                grid[x, z] = new PathNode(x, z, walkable, worldPos);
            }
        }
    }

    public PathNode NodeFromWorldPoint(Vector3 worldPos)
    {
        float px = Mathf.Clamp01((worldPos.x - transform.position.x + gridWorldSize.x * 0.5f) / gridWorldSize.x);
        float pz = Mathf.Clamp01((worldPos.z - transform.position.z + gridWorldSize.y * 0.5f) / gridWorldSize.y);
        int x = Mathf.RoundToInt((sizeX - 1) * px);
        int z = Mathf.RoundToInt((sizeZ - 1) * pz);
        return grid[x, z];
    }

    // Returns up to 8 grid neighbours (cardinal + diagonal)
    public PathNode[] GetNeighbours(PathNode node)
    {
        var result = new System.Collections.Generic.List<PathNode>(8);
        for (int dx = -1; dx <= 1; dx++)
        for (int dz = -1; dz <= 1; dz++)
        {
            if (dx == 0 && dz == 0) continue;
            int nx = node.gridX + dx;
            int nz = node.gridY + dz;
            if (nx >= 0 && nx < sizeX && nz >= 0 && nz < sizeZ)
                result.Add(grid[nx, nz]);
        }
        return result.ToArray();
    }

    public void ResetAllNodes()
    {
        foreach (var node in grid)
            node.Reset();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y));

        if (grid == null) return;
        foreach (var node in grid)
        {
            Gizmos.color = node.walkable ? new Color(0, 1, 0, 0.15f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.05f));
        }
    }
}
