using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotTrace;

namespace Icicle.Benchmarks;

[MemoryDiagnoser]
[DotTraceDiagnoser]
public class WhenAllBenchmark
{
    [Params(10, 100, 1_000)]
    public int Size;

    [Params(1, 2)]
    public double FailureRate;

    [Benchmark(Baseline = true)]
    public async Task BasicWhenAll()
    {
        using var tcs = new CancellationTokenSource();
        var tasks = Enumerable
            .Range(1, Size)
            .Select(async i =>
            {
                if (i % FailureRate == 0)
                {
                    throw new InvalidOperationException("Failure");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10), tcs.Token);
                return i;
            });
        try
        {
            _ = await Task.WhenAll(tasks);
        }
        catch
        {
            //exceptions are expected
        }
    }

    [Benchmark]
    public async Task ColdTaskWithStandardWhenAll()
    {
        using var scope = TaskCancellationScope.Create();
        var tasks = Enumerable
            .Range(1, Size)
            .Select(i =>
            {
                return ColdTask
                    .New(async () =>
                    {
                        if (i % FailureRate == 0)
                        {
                            throw new InvalidOperationException("Failure");
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(10), scope);
                        return i;
                    })
                    .Task;
            });
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            //exceptions are expected
        }
    }
}
