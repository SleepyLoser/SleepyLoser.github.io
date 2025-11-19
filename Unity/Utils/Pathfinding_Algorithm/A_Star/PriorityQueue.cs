using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 泛型优先队列
/// 基于二叉堆算法，适用于所有.NET版本
/// </summary>
/// <typeparam name="TElement">队列元素类型</typeparam>
/// <typeparam name="TPriority">优先级类型，必须实现IComparable<TPriority></typeparam>
public sealed class PriorityQueue<TElement, TPriority> : IEnumerable<(TElement Element, TPriority Priority)> where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> _heap;
    private readonly IComparer<TPriority> _comparer;
    private readonly Dictionary<TElement, int> _elementCounts;

    public enum PriorityQueueOrder
    {
        Min,
        Max
    }

    /// <summary>
    /// 初始化优先队列新实例
    /// </summary>
    /// <param name="capacity">初始容量</param>
    /// <param name="order">队列排序方式</param>
    public PriorityQueue(int capacity = 0, PriorityQueueOrder order = PriorityQueueOrder.Min)
    {
        _heap = capacity > 0 ? new List<(TElement, TPriority)>(capacity) : new List<(TElement, TPriority)>();
        _elementCounts = capacity > 0 ? new Dictionary<TElement, int>(capacity) : new Dictionary<TElement, int>();
        _comparer = order == PriorityQueueOrder.Min ? Comparer<TPriority>.Default : Comparer<TPriority>.Create((x, y) => y.CompareTo(x));
    }

    /// <summary>
    /// 使用自定义比较器初始化
    /// </summary>
    public PriorityQueue(IComparer<TPriority> comparer, int capacity = 0)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        _heap = capacity > 0 ? new List<(TElement, TPriority)>(capacity) : new List<(TElement, TPriority)>();
        _elementCounts = capacity > 0 ? new Dictionary<TElement, int>(capacity) : new Dictionary<TElement, int>();
    }

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;

    public void Enqueue(TElement element, TPriority priority)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        _heap.Add((element, priority));
        
        // 更新元素计数
        if (_elementCounts.ContainsKey(element)) ++_elementCounts[element];
        else _elementCounts[element] = 1;

        HeapifyUp(_heap.Count - 1);
    }

    public TElement Peek()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty");
        
        return _heap[0].Element;
    }

    public TElement Dequeue()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty");

        TElement result = _heap[0].Element;
        RemoveRoot();
        return result;
    }

    public bool TryDequeue(out TElement element, out TPriority priority)
    {
        if (_heap.Count > 0)
        {
            element = _heap[0].Element;
            priority = _heap[0].Priority;
            RemoveRoot();
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    public bool TryPeek(out TElement element, out TPriority priority)
    {
        if (_heap.Count > 0)
        {
            element = _heap[0].Element;
            priority = _heap[0].Priority;
            return true;
        }

        element = default;
        priority = default;
        return false;
    }

    /// <summary>
    /// 检查队列是否包含指定元素
    /// </summary>
    public bool Contains(TElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return _elementCounts.ContainsKey(element);
    }

    /// <summary>
    /// 获取指定元素在队列中的出现次数
    /// </summary>
    public int GetElementCount(TElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        return _elementCounts.TryGetValue(element, out int count) ? count : 0;
    }

    /// <summary>
    /// 移除队列中指定元素的所有实例
    /// </summary>
    public bool RemoveAll(TElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (!_elementCounts.ContainsKey(element))
            return false;

        // 记录要移除的元素数量
        int countToRemove = _elementCounts[element];
        
        // 从堆中移除所有匹配的元素
        _heap.RemoveAll(item => EqualityComparer<TElement>.Default.Equals(item.Element, element));
        
        // 重建堆（因为直接移除破坏了堆结构）
        RebuildHeap();
        
        // 更新计数
        _elementCounts.Remove(element);
        
        return countToRemove > 0;
    }

    /// <summary>
    /// 移除队列中指定元素的一个实例
    /// </summary>
    public bool Remove(TElement element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (!_elementCounts.ContainsKey(element)) return false;

        // 查找元素的第一个出现位置
        int index = -1;
        for (int i = 0; i < _heap.Count; i++)
        {
            if (EqualityComparer<TElement>.Default.Equals(_heap[i].Element, element))
            {
                index = i;
                break;
            }
        }

        if (index == -1) return false;

        // 移除元素
        RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        _heap.Clear();
        _elementCounts.Clear();
    }

    public (TElement Element, TPriority Priority)[] ToArray()
    {
        return _heap.ToArray();
    }

    public IEnumerator<(TElement Element, TPriority Priority)> GetEnumerator()
    {
        return _heap.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void RemoveRoot()
    {
        if (_heap.Count == 0) return;

        var rootElement = _heap[0].Element;
        
        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        
        // 更新元素计数
        UpdateElementCount(rootElement, -1);
        
        if (_heap.Count > 0)
        {
            HeapifyDown(0);
        }
    }

    private void RemoveAt(int index)
    {
        if (index < 0 || index >= _heap.Count) return;

        var element = _heap[index].Element;
        
        // 将最后一个元素移动到当前位置
        int lastIndex = _heap.Count - 1;
        _heap[index] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        
        // 更新元素计数
        UpdateElementCount(element, -1);
        
        // 如果需要，调整堆结构
        if (index < _heap.Count)
        {
            HeapifyUp(index);
            HeapifyDown(index);
        }
    }

    private void UpdateElementCount(TElement element, int delta)
    {
        if (_elementCounts.ContainsKey(element))
        {
            _elementCounts[element] += delta;
            if (_elementCounts[element] <= 0)
            {
                _elementCounts.Remove(element);
            }
        }
    }

    private void RebuildHeap()
    {
        // 简单的堆重建方法：从最后一个非叶子节点开始向下堆化
        for (int i = _heap.Count / 2 - 1; i >= 0; i--)
        {
            HeapifyDown(i);
        }
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            if (_comparer.Compare(_heap[index].Priority, _heap[parentIndex].Priority) >= 0) break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (index < _heap.Count)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int smallest = index;

            if (leftChild < _heap.Count && _comparer.Compare(_heap[leftChild].Priority, _heap[smallest].Priority) < 0)
            {
                smallest = leftChild;
            }

            if (rightChild < _heap.Count && _comparer.Compare(_heap[rightChild].Priority, _heap[smallest].Priority) < 0)
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
    }
}