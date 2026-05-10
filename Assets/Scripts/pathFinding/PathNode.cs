using UnityEngine;

public class PathNode
{
    public int gridX;
    public int gridZ;
    public bool baseWalkable;
    public bool dynamicallyBlocked;

    public Vector3 worldPosition;

    public int gCost;
    public int hCost;
    public PathNode parent;

     public bool walkable
    {
        get { return baseWalkable && !dynamicallyBlocked; }
    }

    public int fCost => gCost + hCost;

    public PathNode(int x, int z, bool isWalkable, Vector3 position)
    {
        gridX = x;
        gridZ = z;
        baseWalkable = isWalkable;
        dynamicallyBlocked = false;
        worldPosition = position;
    }

    public void ResetNode()
    {
        gCost = 999999;
        hCost = 0;
        parent = null;
    }
}