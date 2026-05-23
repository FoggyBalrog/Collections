using System.Collections;
using System.Runtime.CompilerServices;

namespace FoggyBalrog.Collections;

public enum CircularBufferFullBehavior
{
    Throw,
    Overwrite
}

public sealed class CircularBuffer<T> : IReadOnlyList<T>
{
    private readonly T[] _items;
    private readonly CircularBufferFullBehavior _fullBehavior;
    private int _front;
    private int _count;

    public CircularBuffer(int capacity)
        : this(capacity, CircularBufferFullBehavior.Overwrite)
    {
    }

    public CircularBuffer(int capacity, CircularBufferFullBehavior fullBehavior)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        if (!Enum.IsDefined(fullBehavior))
        {
            throw new ArgumentOutOfRangeException(nameof(fullBehavior));
        }

        _items = new T[capacity];
        _fullBehavior = fullBehavior;
    }

    public int Count => _count;

    public int Capacity => _items.Length;

    public bool IsEmpty => _count == 0;

    public bool IsFull => _count == Capacity;

    public T this[int index]
    {
        get
        {
            ValidateIndex(index);
            return _items[GetPhysicalIndex(index)];
        }
        set
        {
            ValidateIndex(index);
            _items[GetPhysicalIndex(index)] = value;
        }
    }

    public void PushBack(T item)
    {
        if (IsFull)
        {
            EnsureCanPush();

            _items[_front] = item;
            _front = Increment(_front);
            return;
        }

        _items[GetPhysicalIndex(_count)] = item;
        _count++;
    }

    public void PushFront(T item)
    {
        if (IsFull)
        {
            EnsureCanPush();

            _front = Decrement(_front);
            _items[_front] = item;
            return;
        }

        _front = Decrement(_front);
        _items[_front] = item;
        _count++;
    }

    public T PopFront()
    {
        if (!TryPopFront(out T item))
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        return item;
    }

    public T PopBack()
    {
        if (!TryPopBack(out T item))
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        return item;
    }

    public bool TryPopFront(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _items[_front];
        ClearSlot(_front);

        _count--;
        _front = _count == 0 ? 0 : Increment(_front);
        return true;
    }

    public bool TryPopBack(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        int index = GetPhysicalIndex(_count - 1);
        item = _items[index];
        ClearSlot(index);

        _count--;
        if (_count == 0)
        {
            _front = 0;
        }

        return true;
    }

    public T PeekFront()
    {
        if (!TryPeekFront(out T item))
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        return item;
    }

    public T PeekBack()
    {
        if (!TryPeekBack(out T item))
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        return item;
    }

    public bool TryPeekFront(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _items[_front];
        return true;
    }

    public bool TryPeekBack(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _items[GetPhysicalIndex(_count - 1)];
        return true;
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (_count == Capacity)
            {
                Array.Clear(_items);
            }
            else if (_count > 0)
            {
                int firstSegmentLength = Math.Min(_count, Capacity - _front);
                Array.Clear(_items, _front, firstSegmentLength);
                Array.Clear(_items, 0, _count - firstSegmentLength);
            }
        }

        _front = 0;
        _count = 0;
    }

    public void CopyTo(T[] destination, int destinationIndex)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if ((uint)destinationIndex > (uint)destination.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationIndex));
        }

        if (destination.Length - destinationIndex < _count)
        {
            throw new ArgumentException("The destination array has insufficient space.", nameof(destination));
        }

        if (_count == 0)
        {
            return;
        }

        int firstSegmentLength = Math.Min(_count, Capacity - _front);
        Array.Copy(_items, _front, destination, destinationIndex, firstSegmentLength);
        Array.Copy(_items, 0, destination, destinationIndex + firstSegmentLength, _count - firstSegmentLength);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _items[GetPhysicalIndex(i)];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void EnsureCanPush()
    {
        if (_fullBehavior == CircularBufferFullBehavior.Throw)
        {
            throw new InvalidOperationException("Buffer is full.");
        }
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetPhysicalIndex(int logicalIndex)
    {
        int index = _front + logicalIndex;
        return index < Capacity ? index : index - Capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Increment(int index) => index + 1 == Capacity ? 0 : index + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Decrement(int index) => index == 0 ? Capacity - 1 : index - 1;

    private void ClearSlot(int index)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _items[index] = default!;
        }
    }
}
