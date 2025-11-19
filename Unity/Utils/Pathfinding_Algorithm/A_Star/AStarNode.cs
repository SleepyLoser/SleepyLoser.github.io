using System;
using UnityEngine;

/// <summary>
/// A* 算法专用节点
/// </summary>
public class AStarNode : IComparable<AStarNode>
{
    /// <summary>
    /// 节点的x轴坐标
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
    public float x
    {
        get { return position.x; }
        set { position = new Vector2(value, y); }
    }

    /// <summary>
    /// 节点的y轴坐标
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
    public float y
    {
        get { return position.y; }
        set { position = new Vector2(x, value); }
    }

    private Vector2 m_Position;
    /// <summary>
    /// 获得节点的二维坐标
    /// </summary>
    /// <value>节点的二维坐标</value>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:命名样式", Justification = "<挂起>")]
    public Vector2 position
    {
        get { return m_Position; }
        set { m_Position = value; }
    }

    /// <summary>
    /// 节点的权值
    /// </summary>
    public float f;
    
    /// <summary>
    /// 起点达到目前遍历节点的距离
    /// </summary>
    public float g;
    
    /// <summary>
    /// 目前遍历的节点到达终点的距离
    /// </summary>
    public float h;

    /// <summary>
    /// 节点状态
    /// </summary>
    public GlobalEnum.NodeStatu nodeStatu;

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
    public AStarNode(float x, float y, GlobalEnum.NodeStatu nodeStatu)
    {
        m_Position = new Vector2(x, y);
        this.nodeStatu = nodeStatu;
    } 
    
    /// <summary>
    /// 自定义升序排列
    /// </summary>
    /// <param name="other">作对比的节点</param>
    /// <returns>节点的权值谁更大, -1 则 other 更大, 1 则当前节点更大</returns>
    public int CompareTo(AStarNode other)
    {
        // 返回值>0时，当前节点排在other节点右边
        // 返回值<0时，当前节点排在other节点左边
        // 可以理解为other节点处于0位置
        // 按F值排序（F值小的优先级高）
        return f.CompareTo(other.f);

        // int fComparison = f.CompareTo(other.f);
        // if (fComparison != 0) return fComparison;
        // // F值相同的情况下，可以按H值排序（H值小的优先级高）
        // // 这有助于在总代价相同时优先选择更接近目标的节点
        // return h.CompareTo(other.h);
    }

    public override bool Equals(object obj)
    {
        return obj is AStarNode other && position.Equals(other.position);
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public static bool operator ==(AStarNode left, AStarNode right)
    {
        return left?.position.Equals(right?.position) ?? right is null;
    }

    public static bool operator !=(AStarNode left, AStarNode right)
    {
        return !(left == right);
    }
}