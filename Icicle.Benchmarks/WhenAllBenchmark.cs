using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotTrace;

namespace Icicle.Benchmarks;

[MemoryDiagnoser]
[DotTraceDiagnoser]
public class WhenAllBenchmark
{
    [Params(100, 1000)]
    public int Size;

    [Params(2, 4)]
    public int AsyncRatio;

    private async ValueTask<int> WorkLoad(int i, CancellationToken token = default)
    {
        if (i % AsyncRatio == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            return 1;
        }

        return 1;
    }

    [Benchmark(Baseline = true)]
    public async Task BasicWhenAll()
    {
        var tasks = Enumerable.Range(1, Size).Select(i => WorkLoad(i).AsTask());
        _ = await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task TaskScopeWhenAll()
    {
        using var scope = new TaskScope.WhenAll();
        for (var i = 0; i < Size; i++)
        {
            var i1 = i;
            _ = scope.Add(c => WorkLoad(i1, c));
        }
        await scope.Run();
    }
}
