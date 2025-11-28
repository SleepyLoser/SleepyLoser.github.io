using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AStar
{
    /// <summary>
    /// 八个方向（根据实际情况制定方向规则）
    /// </summary>
    private static readonly int[ , ] m_Direction = new int[8, 2]{{1, 0}, {1, 1}, {0, 1}, {-1, 1}, {-1, 0}, {-1, -1}, {0, -1}, {1, -1}};

    private static readonly Dictionary<GlobalEnum.DistanceCalculationMethod, Func<Vector2, Vector2, float>> m_DistanceCalculators = new Dictionary<GlobalEnum.DistanceCalculationMethod, Func<Vector2, Vector2, float>>()
    {
        { GlobalEnum.DistanceCalculationMethod.Manhattan, (v1, v2) => ManhattanDistance(v1, v2) },
        { GlobalEnum.DistanceCalculationMethod.Euclidean, (v1, v2) => EuclideanDistance(v1, v2, true) },
        { GlobalEnum.DistanceCalculationMethod.Chebyshev, (v1, v2) => ChebyshevDistance(v1, v2) },
    };
    
    /// <summary>
    /// 曼哈顿距离
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <returns>两个坐标之间的曼哈顿距离</returns>
    private static float ManhattanDistance(Vector2 v1, Vector2 v2)
    {
        return Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y);
    }

    /// <summary>
    /// 欧几里得距离
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <param name="isSquareRootRequired">欧几里得距离是否需要开根（不开根性能更好, 但会导致算法无法找到最优路径）</param>
    /// <returns>两个坐标之间的欧几里得距离或欧几里得距离的平方</returns>
    private static float EuclideanDistance(Vector2 v1, Vector2 v2, bool isSquareRootRequired = true)
    {
        Vector2 difference = v1 - v2;
        float euclideanDistance = Vector2.Dot(difference, difference);
        return isSquareRootRequired ? Mathf.Sqrt(euclideanDistance) : euclideanDistance;
    }

    /// <summary>
    /// 切比雪夫距离
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <returns>两个坐标之间的切比雪夫距离</returns>
    private static float ChebyshevDistance(Vector2 v1, Vector2 v2)
    {
        return Mathf.Max(Mathf.Abs(v1.x - v2.x), Mathf.Abs(v1.y - v2.y));
    }

    /// <summary>
    /// 待评估的节点
    /// </summary>
    private readonly PriorityQueue<AStarNode, AStarNode> m_OpenList = new PriorityQueue<AStarNode, AStarNode>();

    /// <summary>
    /// 已评估的节点
    /// </summary>
    private readonly HashSet<AStarNode> m_CloseList = new HashSet<AStarNode>();

    /// <summary>
    /// 通过 A* 获取两点之间的最短路径
    /// </summary>
    /// <param name="startPosition">起始位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="map">寻路图</param>
    /// <returns>通过 A* 算出的最短路径, 如果找不到最短路径, 返回 null</returns>
    public List<AStarNode> FindPath2D(Vector2 startPosition, Vector2 endPosition, AStarNode[ , ] map)
    {
        // 判断起始点和终点是否合法

        // 是否在地图外（根据实际情况制定）
        float startPositionX = startPosition.x;
        float startPositionY = startPosition.y;
        float endPositionX = endPosition.x;
        float endPositionY = endPosition.y;
        float rangeX = map[map.GetLength(0) - 1, 0].x;
        float rangeY = map[0, map.GetLength(1) - 1].y;
        if
        (
            startPositionX < map[0, 0].x || startPositionX > rangeX || startPositionY < map[0, 0].y || startPositionY > rangeY ||
            endPositionX < map[0, 0].x || endPositionX > rangeX || endPositionY < map[0, 0].y || endPositionY > rangeY
        )
        {
            return null;
        }

        (int startX, int startY) = WorldPositionToAStarPosition(startPosition);
        (int endX, int endY) = WorldPositionToAStarPosition(endPosition);
        
        AStarNode startNode = map[startX, startY];
        AStarNode endNode = map[endX, endY];
        // 是否是阻挡（不可通过）
        if (startNode.nodeStatu == GlobalEnum.NodeStatu.Stop || endNode.nodeStatu == GlobalEnum.NodeStatu.Stop || startNode == endNode)
        {
            return null;
        }

        startNode.father = null;
        startNode.g = 0;
        startNode.h = CalculateH(startNode, endNode, GlobalEnum.DistanceCalculationMethod.Euclidean);
        startNode.f = startNode.g + startNode.h;

        m_OpenList.Enqueue(startNode, startNode);
        while (!m_OpenList.IsEmpty)
        {
            AStarNode currentNode = m_OpenList.Dequeue();

            // 如果找到终点，构建路径并返回
            if (currentNode == endNode)
            {
                List<AStarNode> result = new List<AStarNode>() { currentNode };
                AStarNode pathNode = currentNode;
                
                while (pathNode.father != null)
                {
                    pathNode = pathNode.father;
                    result.Add(pathNode);
                }
                
                result.Reverse();
                return result;
            }

            m_CloseList.Add(currentNode);

            // 获取当前节点的坐标
            (int currentX, int currentY) = WorldPositionToAStarPosition(currentNode.position);

            // 评估所有邻居节点
            for (int i = 0; i < 8; ++i)
            {
                int x = currentX + m_Direction[i, 0];
                int y = currentY + m_Direction[i, 1];
                EvaluateNeighbor(x, y, i, currentNode, endNode, map);
            }
        }

        // 没有找到路径
        return null;
    }

    /// <summary>
    /// 查找邻近节点并将合法的节点添加到 m_OpenList
    /// </summary>
    /// <param name="x">当前节点的x轴坐标</param>
    /// <param name="y">当前节点的y轴坐标</param>
    /// <param name="directionIndex">方向数组的下标</param>
    /// <param name="father">当前节点的父节点</param>
    /// <param name="end">终点</param>
    /// <param name="map">寻路图</param>
    private void EvaluateNeighbor(int x, int y, int directionIndex, AStarNode father, AStarNode end, AStarNode[ , ] map)
    {
        // 判断邻近节点是否合法

        // 1. 是否在地图外
        if (x < 0 || x >= map.GetLength(1) || y < 0 || y >= map.GetLength(0))
        {
            return;
        }

        // 2. 是否是阻挡（不可通过）并且是否在开启或关闭列表中
        AStarNode node = map[x, y];
        if (node.nodeStatu == GlobalEnum.NodeStatu.Stop || m_OpenList.Contains(node) || m_CloseList.Contains(node))
        {
            return;
        }

        // 3. 角色斜向走时(如果允许)，需要判断斜向走时两边的障碍物会不会挡住角色
        if
        (
            m_Direction[directionIndex, 0] == 1 && m_Direction[directionIndex, 1] == 1 && 
            map[x - 1, y].nodeStatu == GlobalEnum.NodeStatu.Stop && map[x, y - 1].nodeStatu == GlobalEnum.NodeStatu.Stop
        )
        {
            return;
        }
        else if 
        (
            m_Direction[directionIndex, 0] == -1 && m_Direction[directionIndex, 1] == 1 &&
            map[x + 1, y].nodeStatu == GlobalEnum.NodeStatu.Stop && map[x, y - 1].nodeStatu == GlobalEnum.NodeStatu.Stop)
        {
            return;
        }
        else if 
        (
            m_Direction[directionIndex, 0] == -1 && m_Direction[directionIndex, 1] == -1 &&
            map[x + 1, y].nodeStatu == GlobalEnum.NodeStatu.Stop && map[x, y + 1].nodeStatu == GlobalEnum.NodeStatu.Stop)
        {
            return;
        }
        else if 
        (
            m_Direction[directionIndex, 0] == 1 && m_Direction[directionIndex, 1] == -1 &&
            map[x - 1, y].nodeStatu == GlobalEnum.NodeStatu.Stop && map[x, y + 1].nodeStatu == GlobalEnum.NodeStatu.Stop)
        {
            return;
        }

        node.father = father;
        node.g = CalculateG(father, node, GlobalEnum.DistanceCalculationMethod.Euclidean);
        node.h = CalculateH(node, end, GlobalEnum.DistanceCalculationMethod.Euclidean);
        node.f = node.g + node.h;

        m_OpenList.Enqueue(node, node);
    }

    /// <summary>
    /// 计算当前节点的g值
    /// </summary>
    /// <param name="father">当前节点的父节点</param>
    /// <param name="currentNode">当前节点</param>
    /// <param name="methodName">节点间的距离计算方式</param>
    /// <returns>当前节点的g值</returns>
    private static float CalculateG(AStarNode father, AStarNode currentNode, GlobalEnum.DistanceCalculationMethod methodName = GlobalEnum.DistanceCalculationMethod.Manhattan)
    {
        return father.g + m_DistanceCalculators[methodName](father.position, currentNode.position);
    }

    /// <summary>
    /// 计算当前节点的h值
    /// </summary>
    /// <param name="currentNode">当前节点</param>
    /// <param name="end">终点</param>
    /// <param name="methodName">节点间的距离计算方式</param>
    /// <returns>当前节点的h值</returns>
    private static float CalculateH(AStarNode currentNode, AStarNode endNode, GlobalEnum.DistanceCalculationMethod methodName = GlobalEnum.DistanceCalculationMethod.Manhattan)
    {
        return m_DistanceCalculators[methodName](currentNode.position, endNode.position);
    }

    /// <summary>
    /// 将世界坐标转化为 A* 算法中寻路图的下标（下标的转换需根据实际情况制定）
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>转换后的 x，y 坐标</returns>
    private static (int, int) WorldPositionToAStarPosition(Vector2 worldPosition)
    {
        return ((int)worldPosition.x, (int)worldPosition.y);
    }
}