public static class GlobalEnum
{
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    public enum NodeStatu
    {
        /// <summary>
        /// 该节点可通过
        /// </summary>
        Walk,
        /// <summary>
        /// 该节点不可通过
        /// </summary>
        Stop,
    }

    /// <summary>
    /// 距离计算方法的枚举
    /// </summary>
    public enum DistanceCalculationMethod
    {
        /// <summary>
        /// 曼哈顿距离
        /// </summary>
        Manhattan,
        /// <summary>
        /// 欧拉距离
        /// </summary>
        Euler,
        /// <summary>
        /// 切比雪夫距离 
        /// </summary>
        Chebyshev,
    }

    public enum PathfindingAlgorithm
    {
        /// <summary>
        /// A* 算法
        /// </summary>
        AStar,

        /// <summary>
        /// JPS 算法
        /// </summary>
        JPS,
    }
}