using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FoggyBalrog.Collections;

public sealed class DisjointSet<T> where T : notnull
{
    private sealed class Partition(T element)
    {
        public readonly List<T> Elements = [element];
    }

    private readonly Dictionary<T, Partition> _partitionByElement;
    private readonly HashSet<Partition> _partitions;

    public DisjointSet(IEnumerable<T> elements) : this(elements, EqualityComparer<T>.Default)
    {
    }

    public DisjointSet(IEnumerable<T> elements, IEqualityComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(comparer);

        int capacity = GetCapacity(elements);
        _partitionByElement = new Dictionary<T, Partition>(capacity, comparer);
        _partitions = new HashSet<Partition>(capacity);

        foreach (T element in elements)
        {
            InsertElement(element);
        }
    }

    public int ElementCount => _partitionByElement.Count;
    public int SetCount => _partitions.Count;

    public void MakeSet(T element)
    {
        InsertElement(element);
    }

    public T FindSet(T element)
    {
        return GetPartition(element, nameof(element)).Elements[0];
    }

    public bool Union(T x, T y)
    {
        Partition partitionX = GetPartition(x, nameof(x));
        Partition partitionY = GetPartition(y, nameof(y));

        if (partitionX == partitionY)
        {
            return false;
        }

        (Partition larger, Partition smaller) = partitionX.Elements.Count >= partitionY.Elements.Count
            ? (partitionX, partitionY)
            : (partitionY, partitionX);

        if (larger.Elements.Count + smaller.Elements.Count > larger.Elements.Capacity)
        {
            larger.Elements.Capacity = larger.Elements.Count + smaller.Elements.Count;
        }

        foreach (T element in smaller.Elements)
        {
            larger.Elements.Add(element);
            _partitionByElement[element] = larger;
        }

        _partitions.Remove(smaller);
        return true;
    }

    public bool InSameSet(T x, T y)
    {
        return GetPartition(x, nameof(x)) == GetPartition(y, nameof(y));
    }

    public IReadOnlyCollection<T> GetSet(T element)
    {
        return GetPartition(element, nameof(element)).Elements.AsReadOnly();
    }

    public IEnumerable<IReadOnlyCollection<T>> GetAllSets()
    {
        foreach (Partition partition in _partitions)
        {
            yield return partition.Elements.AsReadOnly();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Partition GetPartition(T element, string paramName)
    {
        ref Partition partition = ref CollectionsMarshal.GetValueRefOrNullRef(_partitionByElement, element);

        if (Unsafe.IsNullRef(ref partition))
        {
            throw new ArgumentException("Element not found.", paramName);
        }

        return partition;
    }

    private void InsertElement(T element)
    {
        var partition = new Partition(element);

        if (!_partitionByElement.TryAdd(element, partition))
        {
            throw new ArgumentException("Duplicate element.", nameof(element));
        }

        _partitions.Add(partition);
    }

    private static int GetCapacity(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items switch
        {
            ICollection<T> collection => collection.Count,
            IReadOnlyCollection<T> readOnlyCollection => readOnlyCollection.Count,
            _ => 4 // arbitrary small default
        };
    }
}
