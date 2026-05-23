using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace FoggyBalrog.Collections.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class AddressablePriorityQueueBenchmarks
{
    [Params(10_000, 100_000)]
    public int N;

    private (int Key, int Priority)[] _items = null!;
    private int[] _keys = null!;
    private int[] _decreasePriorities = null!;
    private int[] _increasePriorities = null!;
    private int[] _removeKeys = null!;
    private string[] _stringKeys = null!;
    private AddressablePriorityQueue<int, int> _queue = null!;
    private PriorityQueue<int, int> _bclQueue = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var random = new Random(42);
        _items = new (int, int)[N];
        _keys = new int[N];
        _decreasePriorities = new int[N];
        _increasePriorities = new int[N];
        _removeKeys = Enumerable.Range(0, N).ToArray();
        _stringKeys = new string[N];

        for (int i = 0; i < N; i++)
        {
            _items[i] = (i, random.Next());
            _keys[i] = i;
            _decreasePriorities[i] = -i - 1;
            _increasePriorities[i] = int.MaxValue - i;
            _stringKeys[i] = i.ToString();
        }

        Shuffle(_removeKeys, random);
    }

    [IterationSetup(Targets =
    [
        nameof(Addressable_DequeueAll),
        nameof(Addressable_UpdatePriority_DecreaseHeavy),
        nameof(Addressable_UpdatePriority_IncreaseHeavy),
        nameof(Addressable_RemoveArbitrary),
        nameof(Addressable_DijkstraLike)
    ])]
    public void SetupAddressableQueue()
    {
        _queue = new AddressablePriorityQueue<int, int>(_items);
    }

    [IterationSetup(Target = nameof(BclPriorityQueue_DequeueAll))]
    public void SetupBclPriorityQueue()
    {
        _bclQueue = new PriorityQueue<int, int>(_items.Select(static item => (item.Key, item.Priority)));
    }

    [Benchmark(Baseline = true)]
    public int Addressable_EnqueueOnly()
    {
        var queue = new AddressablePriorityQueue<int, int>(N);

        foreach ((int key, int priority) in _items)
        {
            queue.Enqueue(key, priority);
        }

        return queue.Count;
    }

    [Benchmark]
    public int BclPriorityQueue_EnqueueOnly()
    {
        var queue = new PriorityQueue<int, int>(N);

        foreach ((int key, int priority) in _items)
        {
            queue.Enqueue(key, priority);
        }

        return queue.Count;
    }

    [Benchmark]
    public long Addressable_DequeueAll()
    {
        long checksum = 0;

        while (_queue.TryDequeue(out int key, out int priority))
        {
            checksum += key;
            checksum += priority;
        }

        return checksum;
    }

    [Benchmark]
    public long BclPriorityQueue_DequeueAll()
    {
        long checksum = 0;

        while (_bclQueue.TryDequeue(out int key, out int priority))
        {
            checksum += key;
            checksum += priority;
        }

        return checksum;
    }

    [Benchmark]
    public long Addressable_EnqueueDequeueMixed()
    {
        int half = N / 2;
        var queue = new AddressablePriorityQueue<int, int>(N);
        long checksum = 0;

        for (int i = 0; i < half; i++)
        {
            queue.Enqueue(_items[i].Key, _items[i].Priority);
        }

        for (int i = half; i < N; i++)
        {
            checksum += queue.Dequeue();
            queue.Enqueue(_items[i].Key, _items[i].Priority);
        }

        while (queue.TryDequeue(out int key, out _))
        {
            checksum += key;
        }

        return checksum;
    }

    [Benchmark]
    public long Addressable_UpdatePriority_DecreaseHeavy()
    {
        long checksum = 0;

        for (int i = 0; i < N; i++)
        {
            _queue.UpdatePriority(_keys[i], _decreasePriorities[i]);
        }

        while (_queue.TryDequeue(out int key, out _))
        {
            checksum += key;
        }

        return checksum;
    }

    [Benchmark]
    public long Addressable_UpdatePriority_IncreaseHeavy()
    {
        long checksum = 0;

        for (int i = 0; i < N; i++)
        {
            _queue.UpdatePriority(_keys[i], _increasePriorities[i]);
        }

        while (_queue.TryDequeue(out int key, out _))
        {
            checksum += key;
        }

        return checksum;
    }

    [Benchmark]
    public int Addressable_RemoveArbitrary()
    {
        int removed = 0;

        foreach (int key in _removeKeys)
        {
            if (_queue.Remove(key))
            {
                removed++;
            }
        }

        return removed;
    }

    [Benchmark]
    public long Addressable_DijkstraLike()
    {
        long checksum = 0;

        for (int i = 0; i < N; i += 2)
        {
            _queue.UpdatePriority(i, _decreasePriorities[i]);
        }

        while (_queue.TryDequeue(out int key, out int priority))
        {
            checksum += key;
            checksum += priority;
        }

        return checksum;
    }

    [Benchmark]
    public int Addressable_StringKeys_EnqueueOnly()
    {
        var queue = new AddressablePriorityQueue<string, int>(N, priorityComparer: null, keyComparer: StringComparer.Ordinal);

        for (int i = 0; i < N; i++)
        {
            queue.Enqueue(_stringKeys[i], _items[i].Priority);
        }

        return queue.Count;
    }

    private static void Shuffle(int[] values, Random random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
