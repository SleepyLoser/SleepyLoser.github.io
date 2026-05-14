using System;
using System.Collections.Generic;

namespace DarkWood
{
    /// <summary>
    /// 支持快速查找(Contains)和动态更新优先级(UpdatePriority)的优先队列。
    /// 默认实现为最小堆（Min-Heap）。
    /// </summary>
    /// <typeparam name="TElement">元素类型（必须是唯一的，且非空）</typeparam>
    /// <typeparam name="TPriority">优先级类型</typeparam>
    public sealed class PriorityQueue<TElement, TPriority> where TElement : notnull
    {
        private readonly List<(TElement Element, TPriority Priority)> _heap;
        private readonly Dictionary<TElement, int> _indices;
        private readonly IComparer<TPriority> _comparer;

        public int Count => _heap.Count;
        public bool IsEmpty => _heap.Count == 0;

        /// <summary>
        /// 初始化 PriorityQueue 的新实例。
        /// </summary>
        public PriorityQueue() : this(Comparer<TPriority>.Default) { }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            _heap = new List<(TElement, TPriority)>();
            _indices = new Dictionary<TElement, int>();
            _comparer = comparer ?? Comparer<TPriority>.Default;
        }

        /// <summary>
        /// 将元素入队。时间复杂度: O(log N)
        /// </summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            if (_indices.ContainsKey(element))
            {
                throw new InvalidOperationException("元素已存在于队列中，请使用 UpdatePriority 更新。");
            }

            _heap.Add((element, priority));
            int index = _heap.Count - 1;
            _indices[element] = index;

            SiftUp(index);
        }

        /// <summary>
        /// 移除并返回具有最小优先级值的元素。时间复杂度: O(log N)
        /// </summary>
        public TElement Dequeue()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("队列为空。");

            var result = _heap[0].Element;
            RemoveAt(0);
            return result;
        }

        /// <summary>
        /// 返回具有最小优先级值的元素但不移除。时间复杂度: O(1)
        /// </summary>
        public TElement Peek()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("队列为空。");
            return _heap[0].Element;
        }

        /// <summary>
        /// 检查元素是否存在于队列中。时间复杂度: O(1)
        /// </summary>
        public bool Contains(TElement element)
        {
            return _indices.ContainsKey(element);
        }

        /// <summary>
        /// 更新已存在元素的优先级。时间复杂度: O(log N)
        /// </summary>
        public void UpdatePriority(TElement element, TPriority newPriority)
        {
            if (!_indices.TryGetValue(element, out int index))
            {
                throw new KeyNotFoundException("该元素不存在于队列中。");
            }

            TPriority oldPriority = _heap[index].Priority;
            _heap[index] = (element, newPriority);

            int comparison = _comparer.Compare(newPriority, oldPriority);
            
            if (comparison < 0)
            {
                // 新优先级更小（更高），需要上浮
                SiftUp(index);
            }
            else if (comparison > 0)
            {
                // 新优先级更大（更低），需要下沉
                SiftDown(index);
            }
        }

        /// <summary>
        /// 尝试获取元素的当前优先级。
        /// </summary>
        public bool TryGetPriority(TElement element, out TPriority priority)
        {
            if (_indices.TryGetValue(element, out int index))
            {
                priority = _heap[index].Priority;
                return true;
            }

            priority = default!;
            return false;
        }

        // --- 私有辅助方法 ---

        private void RemoveAt(int index)
        {
            int lastIndex = _heap.Count - 1;
            var itemToRemove = _heap[index];
            
            // 交换当前节点与最后一个节点
            Swap(index, lastIndex);
            
            // 移除最后一个节点（即原来的目标节点）
            _heap.RemoveAt(lastIndex);
            _indices.Remove(itemToRemove.Element);

            if (index < lastIndex) // 如果移除的不是最后一个元素
            {
                // 需要重新平衡堆
                SiftDown(index);
                SiftUp(index);
            }
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_comparer.Compare(_heap[index].Priority, _heap[parentIndex].Priority) >= 0)
                {
                    break;
                }

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SiftDown(int index)
        {
            int lastIndex = _heap.Count - 1;
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild <= lastIndex && 
                    _comparer.Compare(_heap[leftChild].Priority, _heap[smallest].Priority) < 0)
                {
                    smallest = leftChild;
                }

                if (rightChild <= lastIndex && 
                    _comparer.Compare(_heap[rightChild].Priority, _heap[smallest].Priority) < 0)
                {
                    smallest = rightChild;
                }

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            (_heap[j], _heap[i]) = (_heap[i], _heap[j]);

            // 关键：同时更新索引字典
            _indices[_heap[i].Element] = i;
            _indices[_heap[j].Element] = j;
        }
    }
}