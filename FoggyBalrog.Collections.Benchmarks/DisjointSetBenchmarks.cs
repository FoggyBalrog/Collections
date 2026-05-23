using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace FoggyBalrog.Collections.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class DisjointSetBenchmarks
{
    [Params(10_000, 100_000, 1_000_000)]
    public int N;

    private DisjointSet<int> _dsFresh = null!;
    private DisjointSet<int> _dsMerged = null!;
    // One large set (first quarter), medium sets of ~10 (second quarter),
    // pairs (third quarter), singletons (fourth quarter).
    private DisjointSet<int> _dsPartial = null!;
    private DisjointSet<int> _ds = null!;
    private (int X, int Y)[] _randomPairs = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dsFresh = new DisjointSet<int>(Enumerable.Range(0, N));

        _dsMerged = new DisjointSet<int>(Enumerable.Range(0, N));
        for (int i = 0; i < N - 1; i++)
        {
            _dsMerged.Union(i, i + 1);
        }

        _dsPartial = new DisjointSet<int>(Enumerable.Range(0, N));
        for (int i = 0; i < N / 4 - 1; i++)
        {
            _dsPartial.Union(i, i + 1);
        }

        for (int i = N / 4; i < N / 2 - 1; i++)
        {
            if ((i - N / 4) % 10 != 9)
            {
                _dsPartial.Union(i, i + 1);
            }
        }

        for (int i = N / 2; i < 3 * N / 4 - 1; i += 2)
        {
            _dsPartial.Union(i, i + 1);
        }

        var rng = new Random(42);
        _randomPairs = new (int, int)[N - 1];
        for (int i = 0; i < N - 1; i++)
        {
            _randomPairs[i] = (rng.Next(0, N), rng.Next(0, N));
        }
    }

    [IterationSetup(Targets = [nameof(Union_Sequential), nameof(Union_Random)])]
    public void IterationSetup()
    {
        _ds = new DisjointSet<int>(Enumerable.Range(0, N));
    }

    // N=1_000_000 takes ~37ms per invocation — below the 100ms IterationSetup threshold,
    // so results at that point carry more noise than Union_Random. Treat as indicative.
    [Benchmark(Baseline = true, Description = "Sequential pairs — best-case locality")]
    public void Union_Sequential()
    {
        for (int i = 0; i < N - 1; i++)
        {
            _ds.Union(i, i + 1);
        }
    }

    [Benchmark]
    public void Union_Random()
    {
        foreach ((int x, int y) in _randomPairs)
        {
            _ds.Union(x, y);
        }
    }

    [Benchmark]
    public bool Union_AlreadySameSet()
    {
        return _dsMerged.Union(0, N / 2);
    }

    [Benchmark]
    public int FindSet()
    {
        return _dsFresh.FindSet(N / 2);
    }

    [Benchmark]
    public bool InSameSet_Hit()
    {
        return _dsMerged.InSameSet(0, N / 2);
    }

    [Benchmark]
    public bool InSameSet_Miss()
    {
        return _dsFresh.InSameSet(0, N / 2);
    }

    [Benchmark]
    public IReadOnlyCollection<int> GetSet_SmallSet()
    {
        return _dsFresh.GetSet(N / 2);
    }

    [Benchmark]
    public IReadOnlyCollection<int> GetSet_LargeSet()
    {
        return _dsMerged.GetSet(N / 2);
    }

    [Benchmark]
    public int GetAllSets_ManySets()
    {
        int count = 0;
        foreach (IReadOnlyCollection<int> _ in _dsFresh.GetAllSets())
        {
            count++;
        }

        return count;
    }

    [Benchmark]
    public int GetAllSets_FewSets()
    {
        int count = 0;
        foreach (IReadOnlyCollection<int> _ in _dsPartial.GetAllSets())
        {
            count++;
        }

        return count;
    }
}
