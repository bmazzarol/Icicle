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

    private int index;
    private TaskCancellationScope scope;

    [Benchmark]
    public async Task ColdTaskWithStandardWhenAll()
    {
        using var scope = TaskCancellationScope.Create();
        var tasks = Enumerable
            .Range(1, Size)
            .Select(i =>
            {
                index = i;
                this.scope = scope;
                return ColdTask
                    .New(
                        static async s =>
                        {
                            if (s.index % s.FailureRate == 0)
                            {
                                throw new InvalidOperationException("Failure");
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(10), s.scope);
                            return s.index;
                        },
                        this
                    )
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
