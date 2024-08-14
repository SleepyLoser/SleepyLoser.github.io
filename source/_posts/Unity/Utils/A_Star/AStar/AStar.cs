using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
    /// <summary>
    /// 距离计算方法的枚举
    /// </summary>
    enum DistanceCalculationMethod
    {
        // 曼哈顿
        Manhattan,
        // 欧拉
        Euler,
        // 切比雪夫
        Chebyshev,
    }

    // 地图的宽高
    int MapWidth, MapHeight;

    // 八个方向（根据实际制定方向规则）
    readonly int[ , ] Direction = new int[8, 2]{{1, 0}, {1, 1}, {0, 1}, {-1, 1}, {-1, 0}, {-1, -1}, {0, -1}, {1, -1}};

    // 地图的节点
    public AStarNode[ , ] Nodes;

    // 开启列表( C# Version >= 10 可使用优先队列)
    List<AStarNode> OpenList = new List<AStarNode>();

    // 关闭列表
    List<AStarNode> CloseList = new List<AStarNode>();

    /// <summary>
    /// 初始化地图信息
    /// </summary>
    /// <param name="width">地图的宽</param>
    /// <param name="height">地图的高</param>
    void InitMap(int width, int height)
    {
        Nodes = new AStarNode[width, height];
        OpenList.Clear();
        CloseList.Clear();

        // 随机创建地图障碍物
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                AStarNode node = new AStarNode(i, j, UnityEngine.Random.Range(0, 100) < 20 ? AStarNode.NodeStatu.Stop : AStarNode.NodeStatu.Walk);
                Nodes[i, j] = node;
            }
        }

        MapWidth = width;
        MapHeight = height;
    }





    /// <summary>
    /// 曼哈顿距离
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <returns>两个坐标之间的曼哈顿距离</returns>
    float ManhattanDistance(Vector2 v1, Vector2 v2)
    {
        return Mathf.Abs(v1.x - v2.x) + Mathf.Abs(v1.y - v2.y);
    }

    /// <summary>
    /// 欧拉距离的平方
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <returns>两个坐标之间的欧拉距离的平方</returns>
    float EulerDistance(Vector2 v1, Vector2 v2)
    {
        // 统一不开根号，这样可以提高精度
        return Mathf.Pow(v1.x - v2.x, 2) + Mathf.Pow(v1.y - v2.y, 2);
    }

    /// <summary>
    /// 切比雪夫距离
    /// </summary>
    /// <param name="v1">坐标1</param>
    /// <param name="v2">坐标2</param>
    /// <returns>两个坐标之间的切比雪夫距离</returns>
    float ChebyshevDistance(Vector2 v1, Vector2 v2)
    {
        return Mathf.Max(Mathf.Abs(v1.x - v2.x), Mathf.Abs(v1.y - v2.y));
    }




    
    /// <summary>
    /// 查找邻近节点并将合法的节点添加到OpenList
    /// </summary>
    /// <param name="x">节点的x轴坐标</param>
    /// <param name="y">节点的y轴坐标</param>
    /// <param name="father">当前节点</param>
    /// <param name="end">终点</param>
    void FindAdjacentNodesAndAddThemToTheOpenList(int x, int y, AStarNode father, AStarNode end, int direction)
    {
        // 判断起始或终点坐标是否合法

        // 1. 是否在地图外
        if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
        {
            return;
        }
        AStarNode node = Nodes[x, y];
        // 2. 是否是阻挡（不可通过）并且是否在开启或关闭列表中
        if (node.nodeStatu == AStarNode.NodeStatu.Stop || OpenList.Contains(node) || CloseList.Contains(node))
        {
            return;
        }
        // 3. 角色斜向走时(如果允许)，需要判断斜向走时两边的障碍物会不会挡住角色
        if (Direction[direction, 0] == 1 && Direction[direction, 1] == 1 && 
        Nodes[x - 1, y].nodeStatu == AStarNode.NodeStatu.Stop && Nodes[x, y - 1].nodeStatu == AStarNode.NodeStatu.Stop)
        {
            return;
        }
        else if (Direction[direction, 0] == -1 && Direction[direction, 1] == 1 &&
        Nodes[x + 1, y].nodeStatu == AStarNode.NodeStatu.Stop && Nodes[x, y - 1].nodeStatu == AStarNode.NodeStatu.Stop)
        {
            return;
        }
        else if (Direction[direction, 0] == -1 && Direction[direction, 1] == -1 &&
        Nodes[x + 1, y].nodeStatu == AStarNode.NodeStatu.Stop && Nodes[x, y + 1].nodeStatu == AStarNode.NodeStatu.Stop)
        {
            return;
        }
        else if (Direction[direction, 0] == 1 && Direction[direction, 1] == -1 &&
        Nodes[x - 1, y].nodeStatu == AStarNode.NodeStatu.Stop && Nodes[x, y + 1].nodeStatu == AStarNode.NodeStatu.Stop)
        {
            return;
        }


        node.father = father;
        node.g = CalculateG(father, node, DistanceCalculationMethod.Euler);
        node.h = CalculateH(node, end, DistanceCalculationMethod.Euler);
        node.f = node.g + node.h;

        OpenList.Add(node);
    }


    
    

    /// <summary>
    /// 计算当前节点的g值
    /// </summary>
    /// <param name="father">当前节点的父节点</param>
    /// <param name="currentNode">目前遍历到的节点</param>
    /// <param name="methodName">节点间的距离计算方式</param>
    /// <returns>当前节点的g值</returns>
    float CalculateG(AStarNode father, AStarNode currentNode, DistanceCalculationMethod methodName = DistanceCalculationMethod.Manhattan)
    {
        if (methodName == DistanceCalculationMethod.Manhattan)
        {
            return father.g + ManhattanDistance(father.Position, currentNode.Position);
        }
        else if (methodName == DistanceCalculationMethod.Euler)
        {
            return father.g + EulerDistance(father.Position, currentNode.Position);
        }
        else if (methodName == DistanceCalculationMethod.Chebyshev)
        {
            return father.g + ChebyshevDistance(father.Position, currentNode.Position);
        }
        return 0;
    }   

    /// <summary>
    /// 计算当前节点的h值
    /// </summary>
    /// <param name="currentNode">目前遍历到的节点</param>
    /// <param name="end">终点</param>
    /// <param name="methodName">节点间的距离计算方式</param>
    /// <returns>当前节点的h值</returns>
    float CalculateH(AStarNode currentNode, AStarNode end, DistanceCalculationMethod methodName = DistanceCalculationMethod.Manhattan)
    {
        if (methodName == DistanceCalculationMethod.Manhattan)
        {
            return ManhattanDistance(currentNode.Position, end.Position);
        }
        else if (methodName == DistanceCalculationMethod.Euler)
        {
            return EulerDistance(currentNode.Position, end.Position);
        }
        else if (methodName == DistanceCalculationMethod.Chebyshev)
        {
            return ChebyshevDistance(currentNode.Position, end.Position);
        }
        return 0;
    }


    /// <summary>
    /// 更新地图信息
    /// </summary>
    public void UpdateMap(){}


    /// <summary>
    /// 通过A*获取两点之间的最短路径
    /// </summary>
    /// <param name="startPosition">起始位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="width">地图的宽</param>
    /// <param name="height">地图的高</param>
    /// <returns>通过A*算出的最短路径, 如果找不到最短路径, 返回null</returns>
    public List<AStarNode> ObtainThePath(Vector2 startPosition, Vector2 endPosition, int width, int height)
    {
        // 判断起始或终点坐标是否合法

        // 1. 是否在地图外
        if (startPosition.x < 0 || startPosition.x >= width || startPosition.y < 0 || startPosition.y >= height
            || endPosition.x < 0 || endPosition.x >= width || endPosition.y < 0 || endPosition.y >= height)
        {
            return null;
        }

        InitMap(width, height);

        // 2. 是否是阻挡（不可通过）
        AStarNode start = Nodes[(int)startPosition.x, (int)startPosition.y];
        AStarNode end = Nodes[(int)endPosition.x, (int)endPosition.y];
        if (start.nodeStatu == AStarNode.NodeStatu.Stop || end.nodeStatu == AStarNode.NodeStatu.Stop)
        {
            return null;
        }

        start.father = null;
        start.f = start.g = start.h = 0;
        CloseList.Add(start);

        return FindPath(start, end);
    }

    /// <summary>
    /// 获取两点之间的最短路径
    /// </summary>
    /// <param name="start">父节点</param>
    /// <param name="end">终点</param>
    /// <returns>两点之间的最短路径, 如果找不到最短路径, 返回null</returns>
    List<AStarNode> FindPath(AStarNode start, AStarNode end)
    {

        for (int i = 0; i < 8; i++)
        {
            int nextX = start.x + Direction[i, 0];
            int nextY = start.y + Direction[i, 1];
            FindAdjacentNodesAndAddThemToTheOpenList(nextX, nextY, start, end, i);
        }

        if (OpenList.Count == 0)
        {
            return null;
        }
        // 需要实现自定义排序，不能直接使用。我选择的方法是AStarNode继承IComparable接口并实现CompareTo函数。
        OpenList.Sort();
        if (OpenList[0] == end)
        {
            AStarNode pathNode = OpenList[0];
            List<AStarNode> result = new List<AStarNode>(){pathNode};
            while (pathNode.father != null)
            {
                pathNode = pathNode.father;
                result.Add(pathNode);
            }
            // 根据实际需要返回正序路径还是倒序路径
            result.Reverse();
            return result;
        }

        CloseList.Add(OpenList[0]);
        start = OpenList[0];
        OpenList.RemoveAt(0);

        return FindPath(start, end);
    }
}





public class AStarNode : IComparable<AStarNode>
{
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    public enum NodeStatu
    {
        // 该节点可通过
        Walk,
        // 该节点不可通过
        Stop,
    }

    /// <summary>
    /// x：节点的x轴坐标，y：节点的y轴坐标
    /// </summary>
    public int x, y;
    Vector2 position = new Vector2();
    /// <summary>
    /// 获得节点的二维坐标
    /// </summary>
    /// <value>节点的二位坐标</value>
    public Vector2 Position
    {
        get
        {
            position.x = x;
            position.y = y;
            return position;
        }
    }

    /// <summary>
    /// f：节点的权值，g：起点达到目前遍历节点的距离， h：目前遍历的节点到达终点的距离
    /// </summary>
    public float f, g, h;

    /// <summary>
    /// 节点状态
    /// </summary>
    public NodeStatu nodeStatu;

    /// <summary>
    /// 该节点的父节点
    /// </summary>
    public AStarNode father;

    /// <summary>
    /// 初始化节点
    /// </summary>
    /// <param name="x">x轴坐标</param>
    /// <param name="y">y轴坐标</param>
    /// <param name="nodeStatu">节点状态</param>
    public AStarNode(int x, int y, NodeStatu nodeStatu)
    {
        this.x = x;
        this.y = y;
        this.nodeStatu = nodeStatu;
    } 
    
    /// <summary>
    /// 自定义升序排列
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(AStarNode other)
    {
        // 返回类型int
        // 返回值>0时，当前成员排在other成员右边
        // 返回值<0时，当前成员排在other成员左边
        // 可以理解为other成员处于0位置
        if (f < other.f) return -1;
        else return 1;
    }
}

