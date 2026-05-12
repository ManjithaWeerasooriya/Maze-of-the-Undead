using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class UCSPathfinder
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

        List<PathNode> openList = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];
            int lowestIndex = 0;

            // Find node with lowest cost (Dijkstra-style)
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

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode, targetHit.position);

            foreach (PathNode neighbour in grid.GetNeighbours(currentNode))
            {
                if ((!neighbour.walkable && neighbour != targetNode) || closedSet.Contains(neighbour))
                {
                    continue;
                }

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

    private static int GetDistance(PathNode nodeA, PathNode nodeB)
    {
        int distanceX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distanceZ = Mathf.Abs(nodeA.gridZ - nodeB.gridZ);

        if (distanceX > distanceZ)
            return 14 * distanceZ + 10 * (distanceX - distanceZ);

        return 14 * distanceX + 10 * (distanceZ - distanceX);
    }
}
