using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    // Returns a list of world-space waypoints from startWorld to targetWorld,
    // or null if no path exists or the grid is unavailable.
    public static List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        PathfindingGrid grid = PathfindingGrid.Instance;
        if (grid == null) return null;

        PathNode startNode  = grid.NodeFromWorldPoint(startWorld);
        PathNode targetNode = grid.NodeFromWorldPoint(targetWorld);

        if (!startNode.walkable || !targetNode.walkable) return null;
        if (startNode == targetNode) return new List<Vector3> { targetWorld };

        grid.ResetAllNodes();

        // Open set — nodes to be evaluated
        var open   = new List<PathNode>();
        var closed = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = Heuristic(startNode, targetNode);
        open.Add(startNode);

        while (open.Count > 0)
        {
            PathNode current = ExtractLowest(open);
            closed.Add(current);

            if (current == targetNode)
                return Retrace(startNode, targetNode, targetWorld);

            foreach (PathNode neighbour in grid.GetNeighbours(current))
            {
                if (!neighbour.walkable || closed.Contains(neighbour)) continue;

                int tentativeG = current.gCost + StepCost(current, neighbour);
                if (tentativeG >= neighbour.gCost) continue;

                neighbour.gCost  = tentativeG;
                neighbour.hCost  = Heuristic(neighbour, targetNode);
                neighbour.parent = current;

                if (!open.Contains(neighbour))
                    open.Add(neighbour);
            }
        }

        return null; // no path found
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    static PathNode ExtractLowest(List<PathNode> list)
    {
        int best = 0;
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].fCost < list[best].fCost ||
               (list[i].fCost == list[best].fCost && list[i].hCost < list[best].hCost))
                best = i;
        }
        PathNode node = list[best];
        list.RemoveAt(best);
        return node;
    }

    // Diagonal-distance heuristic scaled to match StepCost units (10/14)
    static int Heuristic(PathNode a, PathNode b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dz = Mathf.Abs(a.gridY - b.gridY);
        return dx > dz ? 14 * dz + 10 * (dx - dz) : 14 * dx + 10 * (dz - dx);
    }

    // Diagonal moves cost 14 (~√2 * 10), cardinal moves cost 10
    static int StepCost(PathNode a, PathNode b)
    {
        return (Mathf.Abs(a.gridX - b.gridX) + Mathf.Abs(a.gridY - b.gridY)) == 1 ? 10 : 14;
    }

    static List<Vector3> Retrace(PathNode start, PathNode end, Vector3 exactTarget)
    {
        var path = new List<Vector3>();
        PathNode current = end;
        while (current != start)
        {
            path.Add(current.worldPosition);
            current = current.parent;
        }
        path.Reverse();

        // Replace last waypoint with the exact target position so the zombie
        // reaches the player precisely rather than snapping to a grid centre.
        if (path.Count > 0)
            path[path.Count - 1] = exactTarget;

        return path;
    }
}
