using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class BFSPathfinder
{
    public static List<Vector3> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        PathfindingGrid grid = PathfindingGrid.Instance;

        if (grid == null)
        {
            Debug.LogError("No PathfindingGrid found.");
            return null;
        }

        NavMeshHit startHit;
        NavMeshHit targetHit;

        bool startFound = NavMesh.SamplePosition(startPosition, out startHit, 5f, NavMesh.AllAreas);
        bool targetFound = NavMesh.SamplePosition(targetPosition, out targetHit, 5f, NavMesh.AllAreas);

        if (!startFound || !targetFound)
            return null;

        PathNode startNode = grid.NearestWalkableNode(startHit.position);
        PathNode targetNode = grid.NearestWalkableNode(targetHit.position);

        grid.ResetAllNodes();

        Queue<PathNode> openQueue = new Queue<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        openQueue.Enqueue(startNode);

        while (openQueue.Count > 0)
        {
            PathNode currentNode = openQueue.Dequeue();
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode, targetHit.position);

            foreach (PathNode neighbour in grid.GetNeighbours(currentNode))
            {
                if ((!neighbour.walkable && neighbour != targetNode) || closedSet.Contains(neighbour))
                {
                    continue;
                }

                if (!closedSet.Contains(neighbour))
                {
                    neighbour.gCost = currentNode.gCost + 1;
                    neighbour.parent = currentNode;
                    openQueue.Enqueue(neighbour);
                }
            }
        }

        return null;
    }

    private static List<Vector3> RetracePath(PathNode startNode, PathNode endNode, Vector3 exactTargetPosition)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        if (path.Count > 0)
            path[path.Count - 1] = exactTargetPosition;

        return path;
    }
}
