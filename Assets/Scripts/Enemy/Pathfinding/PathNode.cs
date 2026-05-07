public class PathNode
{
    public readonly int gridX, gridY;
    public readonly bool walkable;
    public readonly UnityEngine.Vector3 worldPosition;

    public int  gCost  = int.MaxValue;
    public int  hCost;
    public PathNode parent;

    public int fCost => gCost + hCost;

    public PathNode(int x, int y, bool walkable, UnityEngine.Vector3 pos)
    {
        gridX = x; gridY = y;
        this.walkable      = walkable;
        this.worldPosition = pos;
    }

    public void Reset()
    {
        gCost  = int.MaxValue;
        hCost  = 0;
        parent = null;
    }
}
