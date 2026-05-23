using System.Runtime.CompilerServices;

namespace FoggyBalrog.Collections;

/// <summary>
/// A min-priority queue keyed by <typeparamref name="TKey"/> that supports
/// O(log n) priority updates and removals by key.
/// </summary>
/// <typeparam name="TKey">The unique key used to address an entry.</typeparam>
/// <typeparam name="TPriority">The value used to order entries. Lower values dequeue first.</typeparam>
public sealed class AddressablePriorityQueue<TKey, TPriority>
    where TKey : notnull
{
    private const int Arity = 4;
    private const int Log2Arity = 2;
    private const int MinimumGrow = 4;

    private Entry[] _nodes;
    private int _size;
    private readonly Dictionary<TKey, int> _index;
    private readonly IComparer<TPriority> _priorityComparer;

    private readonly struct Entry(TKey key, TPriority priority)
    {
        public TKey Key { get; } = key;
        public TPriority Priority { get; } = priority;
    }

    public AddressablePriorityQueue()
        : this(initialCapacity: 0, priorityComparer: null, keyComparer: null)
    {
    }

    public AddressablePriorityQueue(int initialCapacity)
        : this(initialCapacity, priorityComparer: null, keyComparer: null)
    {
    }

    public AddressablePriorityQueue(IComparer<TPriority>? priorityComparer)
        : this(initialCapacity: 0, priorityComparer, keyComparer: null)
    {
    }

    public AddressablePriorityQueue(int initialCapacity, IComparer<TPriority>? priorityComparer)
        : this(initialCapacity, priorityComparer, keyComparer: null)
    {
    }

    public AddressablePriorityQueue(
        int initialCapacity,
        IComparer<TPriority>? priorityComparer,
        IEqualityComparer<TKey>? keyComparer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);

        _nodes = initialCapacity == 0 ? [] : new Entry[initialCapacity];
        _index = new Dictionary<TKey, int>(initialCapacity, keyComparer);
        _priorityComparer = priorityComparer ?? Comparer<TPriority>.Default;
    }

    public AddressablePriorityQueue(
        IEnumerable<(TKey Key, TPriority Priority)> items,
        IComparer<TPriority>? priorityComparer = null,
        IEqualityComparer<TKey>? keyComparer = null)
        : this(GetCapacity(items), priorityComparer, keyComparer)
    {
        foreach (var (key, priority) in items)
        {
            EnsureCapacityForOneMore();

            if (!_index.TryAdd(key, _size))
            {
                throw new ArgumentException($"Duplicate key in initial items: {key}", nameof(items));
            }

            _nodes[_size++] = new Entry(key, priority);
        }

        if (_size > 1)
        {
            Heapify();
        }
    }

    public int Count => _size;

    public int Capacity => _nodes.Length;

    public IComparer<TPriority> Comparer => _priorityComparer;

    public IEqualityComparer<TKey> KeyComparer => _index.Comparer;

    public void Enqueue(TKey key, TPriority priority)
    {
        int index = _size;
        if (index < _nodes.Length)
        {
            if (!_index.TryAdd(key, index))
            {
                throw new ArgumentException($"Key already exists: {key}", nameof(key));
            }
        }
        else
        {
            if (_index.ContainsKey(key))
            {
                throw new ArgumentException($"Key already exists: {key}", nameof(key));
            }

            Grow(index + 1);
            _index.Add(key, index);
        }

        MoveUp(new Entry(key, priority), index);
        _size++;
    }

    public bool TryEnqueue(TKey key, TPriority priority)
    {
        int index = _size;
        if (index < _nodes.Length)
        {
            if (!_index.TryAdd(key, index))
            {
                return false;
            }
        }
        else
        {
            if (_index.ContainsKey(key))
            {
                return false;
            }

            Grow(index + 1);
            _index.Add(key, index);
        }

        MoveUp(new Entry(key, priority), index);
        _size++;
        return true;
    }

    public bool EnqueueOrUpdate(TKey key, TPriority priority)
    {
        if (_index.TryGetValue(key, out int index))
        {
            UpdateAt(index, priority);
            return false;
        }

        EnsureCapacityForOneMore();
        _index.Add(key, _size);
        MoveUp(new Entry(key, priority), _size);
        _size++;
        return true;
    }

    public TKey Peek()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return _nodes[0].Key;
    }

    public bool TryPeek(out TKey key, out TPriority priority)
    {
        if (_size == 0)
        {
            key = default!;
            priority = default!;
            return false;
        }

        Entry root = _nodes[0];
        key = root.Key;
        priority = root.Priority;
        return true;
    }

    public bool Contains(TKey key) => _index.ContainsKey(key);

    public bool TryGetPriority(TKey key, out TPriority priority)
    {
        if (_index.TryGetValue(key, out int index))
        {
            priority = _nodes[index].Priority;
            return true;
        }

        priority = default!;
        return false;
    }

    public TKey Dequeue()
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return RemoveAt(0).Key;
    }

    public TKey Dequeue(out TPriority priority)
    {
        if (_size == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        Entry removed = RemoveAt(0);
        priority = removed.Priority;
        return removed.Key;
    }

    public bool TryDequeue(out TKey key, out TPriority priority)
    {
        if (_size == 0)
        {
            key = default!;
            priority = default!;
            return false;
        }

        Entry removed = RemoveAt(0);
        key = removed.Key;
        priority = removed.Priority;
        return true;
    }

    public bool Remove(TKey key)
    {
        if (!_index.TryGetValue(key, out int index))
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public bool Remove(TKey key, out TPriority priority)
    {
        if (!_index.TryGetValue(key, out int index))
        {
            priority = default!;
            return false;
        }

        priority = RemoveAt(index).Priority;
        return true;
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>())
        {
            Array.Clear(_nodes, 0, _size);
        }

        _size = 0;
        _index.Clear();
    }

    public void UpdatePriority(TKey key, TPriority newPriority)
    {
        if (!_index.TryGetValue(key, out int index))
        {
            throw new KeyNotFoundException($"Key not found: {key}");
        }

        UpdateAt(index, newPriority);
    }

    public bool TryUpdatePriority(TKey key, TPriority newPriority)
    {
        if (!_index.TryGetValue(key, out int index))
        {
            return false;
        }

        UpdateAt(index, newPriority);
        return true;
    }

    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (_nodes.Length < capacity)
        {
            Grow(capacity);
        }
        else
        {
            _index.EnsureCapacity(capacity);
        }

        return _nodes.Length;
    }

    public void TrimExcess()
    {
        int threshold = (int)(_nodes.Length * 0.9);
        if (_size < threshold)
        {
            Array.Resize(ref _nodes, _size);
        }

        _index.TrimExcess(_size);
    }

    private void UpdateAt(int index, TPriority newPriority)
    {
        Entry oldNode = _nodes[index];
        int comparison = Compare(newPriority, oldNode.Priority);
        var node = new Entry(oldNode.Key, newPriority);

        if (comparison < 0)
        {
            MoveUp(node, index);
        }
        else if (comparison > 0)
        {
            MoveDown(node, index);
        }
        else
        {
            _nodes[index] = node;
        }
    }

    private Entry RemoveAt(int index)
    {
        Entry removed = _nodes[index];
        _index.Remove(removed.Key);

        int lastIndex = --_size;
        if (index == lastIndex)
        {
            ClearSlot(lastIndex);
            return removed;
        }

        Entry node = _nodes[lastIndex];
        ClearSlot(lastIndex);

        if (index > 0 && Compare(node.Priority, _nodes[GetParentIndex(index)].Priority) < 0)
        {
            MoveUp(node, index);
        }
        else
        {
            MoveDown(node, index);
        }

        return removed;
    }

    private void Heapify()
    {
        for (int index = GetParentIndex(_size - 1); index >= 0; index--)
        {
            MoveDown(_nodes[index], index);
        }
    }

    private void MoveUp(Entry node, int index)
    {
        Entry[] nodes = _nodes;
        IComparer<TPriority> comparer = _priorityComparer;

        while (index > 0)
        {
            int parentIndex = GetParentIndex(index);
            Entry parent = nodes[parentIndex];
            if (comparer.Compare(node.Priority, parent.Priority) >= 0)
            {
                break;
            }

            nodes[index] = parent;
            _index[parent.Key] = index;
            index = parentIndex;
        }

        nodes[index] = node;
        _index[node.Key] = index;
    }

    private void MoveDown(Entry node, int index)
    {
        Entry[] nodes = _nodes;
        int size = _size;
        IComparer<TPriority> comparer = _priorityComparer;

        while (true)
        {
            int childIndex = GetFirstChildIndex(index);
            if (childIndex >= size)
            {
                break;
            }

            Entry minChild = nodes[childIndex];
            int minChildIndex = childIndex;
            int lastChildIndex = Math.Min(childIndex + Arity, size);

            while (++childIndex < lastChildIndex)
            {
                Entry nextChild = nodes[childIndex];
                if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
                {
                    minChild = nextChild;
                    minChildIndex = childIndex;
                }
            }

            if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
            {
                break;
            }

            nodes[index] = minChild;
            _index[minChild.Key] = index;
            index = minChildIndex;
        }

        nodes[index] = node;
        _index[node.Key] = index;
    }

    private void EnsureCapacityForOneMore()
    {
        if (_size == _nodes.Length)
        {
            Grow(_size + 1);
        }
    }

    private void Grow(int minCapacity)
    {
        int newCapacity = _nodes.Length == 0 ? MinimumGrow : 2 * _nodes.Length;
        if ((uint)newCapacity > Array.MaxLength)
        {
            newCapacity = Array.MaxLength;
        }

        if (newCapacity < minCapacity)
        {
            newCapacity = minCapacity;
        }

        Array.Resize(ref _nodes, newCapacity);
        _index.EnsureCapacity(newCapacity);
    }

    private void ClearSlot(int index)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<Entry>())
        {
            _nodes[index] = default;
        }
    }

    private int Compare(TPriority x, TPriority y) => _priorityComparer.Compare(x, y);

    private static int GetParentIndex(int index) => (index - 1) >> Log2Arity;

    private static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;

    private static int GetCapacity(IEnumerable<(TKey Key, TPriority Priority)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items switch
        {
            ICollection<(TKey Key, TPriority Priority)> collection => collection.Count,
            IReadOnlyCollection<(TKey Key, TPriority Priority)> readOnlyCollection => readOnlyCollection.Count,
            _ => 0,
        };
    }
}
