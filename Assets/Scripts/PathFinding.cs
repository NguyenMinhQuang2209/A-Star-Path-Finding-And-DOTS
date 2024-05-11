using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class PathFinding : MonoBehaviour
{
    public static int DIAGLOG = 14;
    public static int STRAIGHT = 10;
    public static PathFinding instance;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public List<Vector2> CaculatePath(int2 start, int2 end)
    {
        float startTime = Time.realtimeSinceStartup;
        List<Vector2> paths = new();
        FindPath(start, end, paths);
        Debug.Log("Time: " + (Time.realtimeSinceStartup - startTime) * 1000f);
        return paths;
    }
    public void FindPath(int2 start, int2 end, List<Vector2> finalPath)
    {
        int2 gridSize = new(200, 200);

        NativeArray<NodePath> pathNodeArray = new NativeArray<NodePath>(gridSize.x * gridSize.y, Allocator.Temp);


        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                NodePath node = new()
                {
                    x = i,
                    y = j,
                    index = CaculateIndex(i, j, gridSize.x),
                    cameFromIndex = -1,
                    gCost = int.MaxValue,
                    hCost = CaculateDistance(new(i, j), end),
                    isWalkable = true
                };
                node.CalculateFCost();
                pathNodeArray[node.index] = node;
            }
        }
        NodePath startNode = pathNodeArray[CaculateIndex(start.x, start.y, gridSize.x)];
        int endNodeIndex = CaculateIndex(end.x, end.y, gridSize.x);

        startNode.gCost = 0;
        startNode.CalculateFCost();
        pathNodeArray[startNode.index] = startNode;

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closeList = new NativeList<int>(Allocator.Temp);

        NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(new int2[]
        {
            new int2(-1,0),
            new int2(+1,0),
            new int2(0,-1),
            new int2(0,+1),
            new int2(+1,-1),
            new int2(-1,+1),
            new int2(+1,+1),
            new int2(-1,-1),
        }, Allocator.Temp);

        openList.Add(startNode.index);
        while (openList.Length > 0)
        {
            int nodeIndex = CaculateLowestFCost(openList, pathNodeArray);
            NodePath currentNode = pathNodeArray[nodeIndex];

            if (currentNode.index == endNodeIndex)
            {
                break;
            }

            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentNode.index)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }
            closeList.Add(currentNode.index);

            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPos = new(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);
                if (!IsPositionInsideGrid(neighbourPos, gridSize))
                {
                    continue;
                }
                int neighbourIndex = CaculateIndex(neighbourPos.x, neighbourPos.y, gridSize.x);

                if (closeList.Contains(neighbourIndex))
                {
                    continue;
                }
                NodePath neighbour = pathNodeArray[neighbourIndex];
                if (!neighbour.isWalkable)
                {
                    closeList.Add(neighbourIndex);
                    continue;
                }
                int tentactiveGCost = currentNode.gCost + CaculateDistance(new(currentNode.x, currentNode.y), neighbourPos);
                if (tentactiveGCost < neighbour.gCost)
                {
                    neighbour.gCost = tentactiveGCost;
                    neighbour.cameFromIndex = currentNode.index;
                    neighbour.hCost = CaculateDistance(neighbourPos, end);
                    neighbour.CalculateFCost();

                    pathNodeArray[neighbour.index] = neighbour;

                    if (!openList.Contains(neighbour.index))
                    {
                        openList.Add(neighbour.index);
                    }
                }
            }
        }

        NodePath endNode = pathNodeArray[endNodeIndex];
        if (endNode.cameFromIndex == -1)
        {

        }
        else
        {
            NativeList<int2> paths = CalculatePath(pathNodeArray, endNode);
            foreach (int2 path in paths)
            {
                finalPath.Add(new(path.x, path.y));
            }
            paths.Dispose();
        }

        openList.Dispose();
        closeList.Dispose();
        pathNodeArray.Dispose();
    }
    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return gridPosition.x >= 0 && gridPosition.y >= 0 && gridPosition.x < gridSize.x && gridPosition.y < gridSize.y;
    }
    private NativeList<int2> CalculatePath(NativeArray<NodePath> pathNodeArray, NodePath endNode)
    {
        NativeList<int2> path = new NativeList<int2>(Allocator.Temp)
            {
                new(endNode.x, endNode.y)
            };

        NodePath currentNode = pathNodeArray[endNode.index];
        while (currentNode.cameFromIndex != -1)
        {
            NodePath cameFromNode = pathNodeArray[currentNode.cameFromIndex];
            path.Add(new(cameFromNode.x, cameFromNode.y));
            currentNode = cameFromNode;
        }
        return path;
    }
    public int CaculateDistance(int2 from, int2 to)
    {
        int x = Mathf.Abs(from.x - to.x);
        int y = Mathf.Abs(from.y - to.y);
        int remain = Mathf.Abs(x - y);
        return DIAGLOG * Mathf.Min(x, y) + STRAIGHT * remain;
    }
    private int CaculateLowestFCost(NativeList<int> list, NativeArray<NodePath> nodeList)
    {
        NodePath node = nodeList[list[0]];
        for (int i = 1; i < list.Length; i++)
        {
            NodePath current = nodeList[list[i]];
            if (current.fCost < node.fCost)
            {
                node = current;
            }
        }
        return node.index;
    }
    public int CaculateIndex(int x, int y, int gridWith)
    {
        return x + y * gridWith;
    }
    private struct NodePath
    {
        public int x;
        public int y;

        public int gCost;
        public int hCost;

        public int index;

        public int fCost;
        public bool isWalkable;
        public int cameFromIndex;

        public void CalculateFCost()
        {
            fCost = hCost + gCost;
        }
    }
}
