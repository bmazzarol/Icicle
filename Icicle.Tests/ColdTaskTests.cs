namespace Icicle.Tests;

public sealed class ColdTaskTests
{
    [Fact(DisplayName = "ColdTask should not execute until awaited")]
    public async Task Case1()
    {
        var counter = 0;

        var lt = ColdTask.New(LazyAction);
        await Task.Delay(2);
        counter.Should().Be(0);

        var result = await lt;
        result.Should().Be(42);
        return;

        async Task<int> LazyAction()
        {
            counter++;
            await Task.Delay(1);
            return await SecondLazyAction() - 1;
        }

        async Task<int> SecondLazyAction()
        {
            counter++;
            await Task.Delay(1);
            return 43;
        }
    }

    [Fact(DisplayName = "ColdTask can be used in standard WhenAll")]
    public async Task Case2()
    {
        var ct1 = LazyAction();

        var t1 = await ct1;
        var t2 = await ct1;

        t1.Should().Be(t2);

        var results = await Task.WhenAll(ct1.Task, SecondLazyAction().Task);
        results.Should().BeEquivalentTo([42, 43]);
        return;

        ColdTask<int> LazyAction() =>
            ColdTask.New(async () =>
            {
                await Task.Delay(1);
                return await SecondLazyAction() - 1;
            });

        ColdTask<int> SecondLazyAction() =>
            ColdTask.New(async () =>
            {
                await Task.Delay(1);
                return 43;
            });
    }

    [Fact(DisplayName = "ColdTask can be used in standard WhenAny")]
    public async Task Case3()
    {
        var firstTask = await Task.WhenAny(LazyAction().Task, SecondLazyAction().Task);
        var result = await firstTask;

        result.Should().Be(43);
        return;

        ColdTask<int> LazyAction() =>
            ColdTask.New(async () =>
            {
                await Task.Delay(10);
                return await SecondLazyAction() - 1;
            });

        ColdTask<int> SecondLazyAction() =>
            ColdTask.New(async () =>
            {
                await Task.Delay(1);
                return 43;
            });
    }

    [Fact(DisplayName = "ColdTask can be used in a scope")]
    public async Task Case4()
    {
        var counter = 0;

        using var scope = TaskCancellationScope.Create();
        var a1 = LazyAction(scope);
        var a2 = LazyAction(scope);
        var a3 = LazyAction(scope, throwException: true);

        await ThrowsAsync<InvalidOperationException>(
            async () => await Task.WhenAll(a1.Task, a2.Task, a3.Task)
        );

        a1.Task.IsCanceled.Should().BeTrue();
        a2.Task.IsCanceled.Should().BeTrue();
        a3.Task.IsCanceled.Should().BeFalse();
        a3.Task.Exception.Should().BeOfType<AggregateException>();

        return;

        ColdTask<int> LazyAction(CancellationToken token, bool throwException = false) =>
            ColdTask.New(async () =>
            {
                counter++;
                await Task.Yield();
                if (throwException)
                {
                    throw new InvalidOperationException();
                }

                await Task.Delay(1000, token);

                return counter;
            });
    }
}
