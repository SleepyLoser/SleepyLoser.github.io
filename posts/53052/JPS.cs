using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class JPS
{
    /// <summary>
    /// 八个方向（根据实际情况制定方向规则）
    /// </summary>
    private static readonly int[ , ] m_Direction = new int[8, 2]{{1, 0}, {1, 1}, {0, 1}, {-1, 1}, {-1, 0}, {-1, -1}, {0, -1}, {1, -1}};

    /// <summary>
    /// 启发式枚举与启发式函数之间的映射
    /// </summary>
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
    /// 寻路图
    /// </summary>
    private JPSNode[ , ] m_Map;

    /// <summary>
    /// 地图的宽
    /// </summary>
    private int m_MapWidth;

    /// <summary>
    /// 地图的高
    /// </summary>
    private int m_MapHeight;

    /// <summary>
    /// 待评估的节点
    /// </summary>
    private readonly PriorityQueue<JPSNode, JPSNode> m_OpenList = new PriorityQueue<JPSNode, JPSNode>();

    /// <summary>
    /// 已评估的节点
    /// </summary>
    private readonly HashSet<JPSNode> m_CloseList = new HashSet<JPSNode>();

    /// <summary>
    /// 通过 JPS 获取两点之间的最短路径
    /// </summary>
    /// <param name="startPosition">起点</param>
    /// <param name="endPosition">终点</param>
    /// <param name="map">寻路图</param>
    /// <returns>通过 JPS 算出的最短路径, 如果找不到最短路径, 返回 null</returns>
    public List<JPSNode> FindPath2D(Vector2 startPosition, Vector2 endPosition, JPSNode[ , ] map)
    {
        // 判断起始点和终点是否合法
        m_Map = map;
        m_MapWidth = map.GetLength(0);
        m_MapHeight = map.GetLength(1);

        (int startX, int startY) = WorldPositionToJPSPosition(startPosition);
        (int endX, int endY) = WorldPositionToJPSPosition(endPosition);

        if (!IsWalkable(startX, startY) || !IsWalkable(endX, endY)) return null;

        JPSNode startNode = map[startX, startY];
        JPSNode endNode = map[endX, endY];
        if (startNode == endNode) return null;

        startNode.father = null;
        startNode.g = 0;
        startNode.h = CalculateH(startNode, endNode, GlobalEnum.DistanceCalculationMethod.Euclidean);
        startNode.f = startNode.g + startNode.h;

        m_OpenList.Enqueue(startNode, startNode);
        while (!m_OpenList.IsEmpty)
        {
            JPSNode currentNode = m_OpenList.Dequeue();
            m_CloseList.Add(currentNode);

            // 如果找到终点，构建路径并返回
            if (currentNode == endNode)
            {
                List<JPSNode> result = new List<JPSNode>() { currentNode };
                JPSNode pathNode = currentNode;
                
                while (pathNode.father != null)
                {
                    pathNode = pathNode.father;
                    result.Add(pathNode);
                }
                
                result.Reverse();
                return result;
            }

            // 评估邻居
            (int currentNodeX, int currentNodeY) = WorldPositionToJPSPosition(currentNode.position);
            List<(int dx, int dy)> neighbors = EvaluateNeighbor(currentNodeX, currentNodeY, currentNode);

            // 寻找跳跃点
            for (int i = 0; i < neighbors.Count; ++i)
            {
                JPSNode jumpNode = SearchJumpNode(currentNodeX, currentNodeY, neighbors[i].dx, neighbors[i].dy, endNode);
                if (jumpNode != null && !m_CloseList.Contains(jumpNode))
                {
                    jumpNode.father = currentNode;
                    // 计算新的 G 值
                    float newG = CalculateG(currentNode, jumpNode, GlobalEnum.DistanceCalculationMethod.Euclidean);
                    // 如果跳跃点已在开放列表中，且新的 G 值更小，则更新节点信息
                    if (m_OpenList.Contains(jumpNode))
                    {
                        if (newG < jumpNode.g)
                        {
                            jumpNode.g = newG;
                            jumpNode.f = jumpNode.g + jumpNode.h;
                            m_OpenList.UpdatePriority(jumpNode, jumpNode);
                        }
                    }
                    // 新节点：初始化并加入开放列表
                    else
                    {
                        jumpNode.g = newG;
                        jumpNode.h = CalculateH(jumpNode, endNode, GlobalEnum.DistanceCalculationMethod.Euclidean);
                        jumpNode.f = jumpNode.g + jumpNode.h;
                        m_OpenList.Enqueue(jumpNode, jumpNode);
                    }
                }
            }
        }

        // 没有找到路径
        return null;
    }

    /// <summary>
    /// 评估当前节点的邻居方向
    /// </summary>
    /// <param name="nodeX">当前需要评估的节点的 X 坐标</param>
    /// <param name="nodeY">当前需要评估的节点的 Y 坐标</param>
    /// <param name="node">当前需要评估的节点</param>
    /// <returns>需要探索的邻居方向偏移量列表(dx, dy)</returns>
    private List<(int dx, int dy)> EvaluateNeighbor(int nodeX, int nodeY, JPSNode node)
    {
        List<(int dx, int dy)> neighbors = new List<(int dx, int dy)>();

        // 如果是起点（没有父节点），返回所有 8 个可走方向
        if (node.father == null)
        {
            for (int i = 0; i < 8; i++)
            {
                int dx = m_Direction[i, 0];
                int dy = m_Direction[i, 1];
                if (IsWalkable(nodeX + dx, nodeY + dy)) neighbors.Add((dx, dy));
            }
            return neighbors;
        }

        // 计算移动方向
        (int nodeFatherX, int nodeFatherY) = WorldPositionToJPSPosition(node.father.position);
        int dxDir = Mathf.Clamp(nodeX - nodeFatherX, -1, 1);
        int dyDir = Mathf.Clamp(nodeY - nodeFatherY, -1, 1);

        // 辅助函数：防止穿墙判定
        bool CanMoveDiagonal(int cx, int cy, int dx, int dy)
        {
            // 目标必须可走
            if (!IsWalkable(cx + dx, cy + dy)) return false;
            // 防穿墙：水平或垂直方向至少有一个可走（宽松模式）
            // 若要严格模式（不能蹭墙角），改为 && 
            return IsWalkable(cx + dx, cy) || IsWalkable(cx, cy + dy);
        }

        // 1. 对角线移动
        if (dxDir != 0 && dyDir != 0)
        {
            // 自然邻居：前方，水平分量，垂直分量
            if (CanMoveDiagonal(nodeX, nodeY, dxDir, dyDir)) neighbors.Add((dxDir, dyDir));
            if (IsWalkable(nodeX + dxDir, nodeY)) neighbors.Add((dxDir, 0));
            if (IsWalkable(nodeX, nodeY + dyDir)) neighbors.Add((0, dyDir));

            // 强迫邻居 (Forced Neighbor)
            // 只有当反方向水平阻挡，且该方向的对角线可达时，该对角线是强制邻居
            if (!IsWalkable(nodeX - dxDir, nodeY) && CanMoveDiagonal(nodeX, nodeY, -dxDir, dyDir)) neighbors.Add((-dxDir, dyDir));
            
            // 只有当反方向垂直阻挡，且该方向的对角线可达时，该对角线是强制邻居
            if (!IsWalkable(nodeX, nodeY - dyDir) && CanMoveDiagonal(nodeX, nodeY, dxDir, -dyDir)) neighbors.Add((dxDir, -dyDir));
        }
        // 2. 直线移动
        else
        {
            if (dxDir != 0) // 水平移动
            {
                if (IsWalkable(nodeX + dxDir, nodeY))
                {
                    neighbors.Add((dxDir, 0));
                    // 检查上方强迫邻居
                    if (!IsWalkable(nodeX, nodeY + 1) && CanMoveDiagonal(nodeX, nodeY, dxDir, 1)) neighbors.Add((dxDir, 1));
                    // 检查下方强迫邻居
                    if (!IsWalkable(nodeX, nodeY - 1) && CanMoveDiagonal(nodeX, nodeY, dxDir, -1)) neighbors.Add((dxDir, -1));
                }
            }
            else // 垂直移动
            {
                if (IsWalkable(nodeX, nodeY + dyDir))
                {
                    neighbors.Add((0, dyDir));
                    // 检查右侧强迫邻居
                    if (!IsWalkable(nodeX + 1, nodeY) && CanMoveDiagonal(nodeX, nodeY, 1, dyDir)) neighbors.Add((1, dyDir));
                    // 检查左侧强迫邻居
                    if (!IsWalkable(nodeX - 1, nodeY) && CanMoveDiagonal(nodeX, nodeY, -1, dyDir)) neighbors.Add((-1, dyDir));
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// JPS 核心：跳跃函数
    /// </summary>
    /// <param name="cx">当前位置 x</param>
    /// <param name="cy">当前位置 y</param>
    /// <param name="dx">方向 x</param>
    /// <param name="dy">方向 y</param>
    /// <param name="endNode">终点</param>
    /// <returns>找到的跳点或 null</returns>
    private JPSNode SearchJumpNode(int cx, int cy, int dx, int dy, JPSNode endNode)
    {
        while (true)
        {
            // --- 1. 移动前置检查（防穿墙逻辑） ---
            // 如果是对角线移动，必须确保没有“卡墙角”
            if (dx != 0 && dy != 0)
            {
                // 规则：前往 (cx+dx, cy+dy) 时，(cx+dx, cy) 和 (cx, cy+dy) 不能同时阻挡（宽松）
                // 或者：只要有一个阻挡就不能走（严格）
                // 这里使用相对宽松的规则：只要有一个方向是通的就可以走，但如果都堵住了就不能走
                // 同时也隐含检查了目标点 (cx+dx, cy+dy) 是否可走
                
                // 检查目标点是否可走
                if (!IsWalkable(cx + dx, cy + dy)) return null;

                // 检查墙角阻挡 (防穿墙)
                // 如果右边是墙 且 下边是墙，则不能向右下穿过
                if (!IsWalkable(cx + dx, cy) && !IsWalkable(cx, cy + dy)) return null;
                
                // 如果需要更严格的物理碰撞（只要蹭到墙角就不能走），使用下面这行：
                // if (!IsWalkable(cx + dx, cy) || !IsWalkable(cx, cy + dy)) return null;
            }
            else
            {
                // 直线移动，仅检查目标点
                if (!IsWalkable(cx + dx, cy + dy)) return null;
            }

            // --- 2. 执行移动 ---
            cx += dx;
            cy += dy;

            JPSNode currentNode = m_Map[cx, cy];

            // --- 3. 检查是否到达终点 ---
            if (currentNode == endNode) return currentNode;

            // --- 4. 检查强制邻居 (Forced Neighbor) ---
            
            // 4.1 对角线移动 (dx != 0, dy != 0)
            if (dx != 0 && dy != 0)
            {
                // 检查逻辑：
                // 如果反方向水平受阻 (x-dx, y)，但该受阻点的“前方” (x-dx, y+dy) 可走 -> 发现强制邻居
                bool forcedH = !IsWalkable(cx - dx, cy) && IsWalkable(cx - dx, cy + dy);
                // 如果反方向垂直受阻 (x, y-dy)，但该受阻点的“前方” (x+dx, y-dy) 可走 -> 发现强制邻居
                bool forcedV = !IsWalkable(cx, cy - dy) && IsWalkable(cx + dx, cy - dy);

                if (forcedH || forcedV)
                {
                    return currentNode;
                }

                // --- 递归检查水平和垂直分量 ---
                // 只有当当前点本身是合法的位置时，才有资格去检查分量
                // (dx, 0) 和 (0, dy) 的检查不会产生无限递归，深度为 1
                if (SearchJumpNode(cx, cy, dx, 0, endNode) != null || 
                    SearchJumpNode(cx, cy, 0, dy, endNode) != null)
                {
                    return currentNode;
                }
            }
            // 4.2 直线移动
            else
            {
                // 水平移动 (dx != 0)
                if (dx != 0)
                {
                    // 上方有墙 且 dx 方向的上方可走 || 下方有墙 且 dx 方向的下方可走
                    if ((!IsWalkable(cx, cy + 1) && IsWalkable(cx + dx, cy + 1)) ||
                        (!IsWalkable(cx, cy - 1) && IsWalkable(cx + dx, cy - 1)))
                    {
                        return currentNode;
                    }
                }
                // 垂直移动 (dy != 0)
                else
                {
                    // 右侧有墙 且 右侧 dy 方向可走 || 左侧有墙 且 左侧 dy 方向可走
                    if ((!IsWalkable(cx + 1, cy) && IsWalkable(cx + 1, cy + dy)) ||
                        (!IsWalkable(cx - 1, cy) && IsWalkable(cx - 1, cy + dy)))
                    {
                        return currentNode;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断坐标点是否合法并且可行走
    /// </summary>
    /// <param name="x">x轴</param>
    /// <param name="y">y轴</param>
    /// <returns>坐标点是否合法并且可行走</returns>
    private bool IsWalkable(int x, int y)
    {
        return x >= 0 && x < m_MapWidth && y >= 0 && y < m_MapHeight && m_Map[x, y].nodeStatu != GlobalEnum.NodeStatu.Stop;
    }

    /// <summary>
    /// 计算当前节点的g值
    /// </summary>
    /// <param name="father">当前节点的父节点</param>
    /// <param name="currentNode">当前节点</param>
    /// <param name="methodName">节点间的距离计算方式</param>
    /// <returns>当前节点的g值</returns>
    private static float CalculateG(JPSNode father, JPSNode currentNode, GlobalEnum.DistanceCalculationMethod methodName = GlobalEnum.DistanceCalculationMethod.Manhattan)
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
    private static float CalculateH(JPSNode currentNode, JPSNode endNode, GlobalEnum.DistanceCalculationMethod methodName = GlobalEnum.DistanceCalculationMethod.Manhattan)
    {
        return m_DistanceCalculators[methodName](currentNode.position, endNode.position);
    }

    /// <summary>
    /// 将世界坐标转化为 JPS 算法中寻路图的下标（下标的转换需根据实际情况制定）
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <returns>转换后的 x，y 坐标</returns>
    private static (int, int) WorldPositionToJPSPosition(Vector2 worldPosition)
    {
        return ((int)worldPosition.x, (int)worldPosition.y);
    }
}