using UnityEngine;

public class PathNode
{
    public int gridX;
    public int gridZ;
    public bool walkable;
    public Vector3 worldPosition;

    public int gCost;
    public int hCost;
    public PathNode parent;

    public int fCost => gCost + hCost;

    public PathNode(int x, int z, bool isWalkable, Vector3 position)
    {
        gridX = x;
        gridZ = z;
        walkable = isWalkable;
        worldPosition = position;
    }

    public void ResetNode()
    {
        gCost = 999999;
        hCost = 0;
        parent = null;
    }
}