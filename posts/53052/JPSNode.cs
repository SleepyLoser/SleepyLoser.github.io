using System;
using UnityEngine;

/// <summary>
/// JPS 算法专用节点
/// </summary>
public sealed class JPSNode : IComparable<JPSNode>
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
    public JPSNode father;

    public JPSNode(float x, float y, GlobalEnum.NodeStatu nodeStatu)
    {
        m_Position = new Vector2(x, y);
        this.nodeStatu = nodeStatu;
    }

    public int CompareTo(JPSNode other)
    {
        return f.CompareTo(other.f);
    }

    public override bool Equals(object obj)
    {
        return obj is JPSNode other && position.Equals(other.position);
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }

    public static bool operator ==(JPSNode left, JPSNode right)
    {
        return left?.position.Equals(right?.position) ?? right is null;
    }

    public static bool operator !=(JPSNode left, JPSNode right)
    {
        return !(left == right);
    }
}