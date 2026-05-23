using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace FoggyBalrog.Collections.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CircularBufferBenchmarks
{
    [Params(10_000, 100_000)]
    public int N;

    private int[] _items = null!;
    private int[] _destination = null!;
    private CircularBuffer<int> _buffer = null!;
    private Queue<int> _queue = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _items = new int[N];
        _destination = new int[N];

        for (int i = 0; i < N; i++)
        {
            _items[i] = i;
        }
    }

    [IterationSetup(Targets =
    [
        nameof(Circular_PopFrontAll),
        nameof(Circular_PopBackAll),
        nameof(Circular_IndexerRead),
        nameof(Circular_CopyTo),
        nameof(Circular_Enumerate)
    ])]
    public void SetupCircularBuffer()
    {
        _buffer = new CircularBuffer<int>(N);

        foreach (int item in _items)
        {
            _buffer.PushBack(item);
        }
    }

    [IterationSetup(Target = nameof(Queue_DequeueAll))]
    public void SetupQueue()
    {
        _queue = new Queue<int>(_items);
    }

    [Benchmark(Baseline = true)]
    public int Circular_PushBackOnly()
    {
        var buffer = new CircularBuffer<int>(N, CircularBufferFullBehavior.Throw);

        foreach (int item in _items)
        {
            buffer.PushBack(item);
        }

        return buffer.Count;
    }

    [Benchmark]
    public int Queue_EnqueueOnly()
    {
        var queue = new Queue<int>(N);

        foreach (int item in _items)
        {
            queue.Enqueue(item);
        }

        return queue.Count;
    }

    [Benchmark]
    public int Circular_PushFrontOnly()
    {
        var buffer = new CircularBuffer<int>(N, CircularBufferFullBehavior.Throw);

        foreach (int item in _items)
        {
            buffer.PushFront(item);
        }

        return buffer.Count;
    }

    [Benchmark]
    public long Circular_PushBackOverwriteSteadyState()
    {
        var buffer = new CircularBuffer<int>(N);
        long checksum = 0;

        foreach (int item in _items)
        {
            buffer.PushBack(item);
        }

        foreach (int item in _items)
        {
            buffer.PushBack(item);
            checksum += buffer.PeekFront();
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_PushFrontOverwriteSteadyState()
    {
        var buffer = new CircularBuffer<int>(N);
        long checksum = 0;

        foreach (int item in _items)
        {
            buffer.PushBack(item);
        }

        foreach (int item in _items)
        {
            buffer.PushFront(item);
            checksum += buffer.PeekBack();
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_PushBackPopFrontMixed()
    {
        int half = N / 2;
        var buffer = new CircularBuffer<int>(N);
        long checksum = 0;

        for (int i = 0; i < half; i++)
        {
            buffer.PushBack(_items[i]);
        }

        for (int i = half; i < N; i++)
        {
            checksum += buffer.PopFront();
            buffer.PushBack(_items[i]);
        }

        while (buffer.TryPopFront(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Queue_EnqueueDequeueMixed()
    {
        int half = N / 2;
        var queue = new Queue<int>(N);
        long checksum = 0;

        for (int i = 0; i < half; i++)
        {
            queue.Enqueue(_items[i]);
        }

        for (int i = half; i < N; i++)
        {
            checksum += queue.Dequeue();
            queue.Enqueue(_items[i]);
        }

        while (queue.TryDequeue(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_PushFrontPopBackMixed()
    {
        int half = N / 2;
        var buffer = new CircularBuffer<int>(N);
        long checksum = 0;

        for (int i = 0; i < half; i++)
        {
            buffer.PushFront(_items[i]);
        }

        for (int i = half; i < N; i++)
        {
            checksum += buffer.PopBack();
            buffer.PushFront(_items[i]);
        }

        while (buffer.TryPopBack(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_PopFrontAll()
    {
        long checksum = 0;

        while (_buffer.TryPopFront(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Queue_DequeueAll()
    {
        long checksum = 0;

        while (_queue.TryDequeue(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_PopBackAll()
    {
        long checksum = 0;

        while (_buffer.TryPopBack(out int item))
        {
            checksum += item;
        }

        return checksum;
    }

    [Benchmark]
    public long Circular_IndexerRead()
    {
        long checksum = 0;

        for (int i = 0; i < _buffer.Count; i++)
        {
            checksum += _buffer[i];
        }

        return checksum;
    }

    [Benchmark]
    public long Array_IndexerRead()
    {
        long checksum = 0;

        for (int i = 0; i < _items.Length; i++)
        {
            checksum += _items[i];
        }

        return checksum;
    }

    [Benchmark]
    public int Circular_CopyTo()
    {
        _buffer.CopyTo(_destination, 0);
        return _destination[N - 1];
    }

    [Benchmark]
    public int Array_CopyTo()
    {
        Array.Copy(_items, _destination, _items.Length);
        return _destination[N - 1];
    }

    [Benchmark]
    public long Circular_Enumerate()
    {
        long checksum = 0;

        foreach (int item in _buffer)
        {
            checksum += item;
        }

        return checksum;
    }
}
