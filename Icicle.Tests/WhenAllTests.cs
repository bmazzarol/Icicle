namespace Icicle.Tests;

public class WhenAllTests
{
    [Fact(DisplayName = "Many `Add` operations can be done")]
    public async Task Case1()
    {
        using var scope = new TaskScope.WhenAll();
        var st1 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            return 1;
        });
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), token);
            return "2";
        });
        var st3 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), token);
            return 3.0;
        });
        var st4 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), token);
        });

        var result = await scope.Run(token: TestContext.Current.CancellationToken);

        st1.ThrowIfFaulted(result);
        st2.ThrowIfFaulted(result);
        st3.ThrowIfFaulted(result);
        st4.ThrowIfFaulted(result);

        Assert.Equal(ResultHandleState.Succeeded, st1.GetState(result));
        Assert.Equal(ResultHandleState.Succeeded, st2.GetState(result));

        Assert.Equal(1, st1.Value(result));
        var value2 = await st1.AsValueTask(result);
        Assert.Equal(1, value2);
        Assert.Equal("2", st2.Value(result));
        Assert.Equal(3.0, st3.Value(result));
        await st4.AsValueTask(result);
    }

    [Fact(DisplayName = "`Run` with no `Add` calls is a no-op")]
    public async Task Case2()
    {
        using var scope = new TaskScope.WhenAll();
        var result = await scope.Run(token: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact(DisplayName = "`Run` can only be called once")]
    public async Task Case3()
    {
        using var scope = new TaskScope.WhenAll();
        Assert.False(scope.IsRunTriggered);
        await scope.Run(token: TestContext.Current.CancellationToken);
        Assert.True(scope.IsRunTriggered);
        var exception = await Assert.ThrowsAsync<TaskScopeCompletedException>(
            async () => await scope.Run(token: TestContext.Current.CancellationToken)
        );
        Assert.Equal("The current `TaskScope` has already completed", exception.Message);
    }

    [Fact(DisplayName = "Many `Add` operations can be done windowed")]
    public async Task Case4()
    {
        using var scope = new TaskScope.WhenAll(windowSize: 2);
        var st1 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            return 1;
        });
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), token);
            return "2";
        });
        var st3 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), token);
            return 3.0;
        });

        var result = await scope.Run(token: TestContext.Current.CancellationToken);

        Assert.Equal(ResultHandleState.Succeeded, st1.GetState(result));
        Assert.Equal(ResultHandleState.Succeeded, st2.GetState(result));
        Assert.Equal(ResultHandleState.Succeeded, st3.GetState(result));

        Assert.Equal(1, st1.Value(result));
        Assert.Equal("2", st2.Value(result));
        Assert.Equal(3.0, st3.Value(result));
    }

    [Fact(DisplayName = "A whole `TaskScope` can be canceled")]
    public async Task Case6()
    {
        using var scope = new TaskScope.WhenAll();
        var st1 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), token);
            return 1;
        });
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), token);
            return "2";
        });
        var st3 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), token);
            return 3.0;
        });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var result = await scope.Run(token: cts.Token);

        Assert.Equal(ResultHandleState.Terminated, st1.GetState(result));
        Assert.Throws<TaskCanceledException>(() => st1.Value(result));
        Assert.Equal(ResultHandleState.Terminated, st2.GetState(result));
        Assert.Throws<TaskCanceledException>(() => st2.Value(result));
        Assert.Equal(ResultHandleState.Terminated, st3.GetState(result));
        Assert.Throws<TaskCanceledException>(() => st3.Value(result));
    }

    [Fact(DisplayName = "One failure will cancel all other tasks")]
    public async Task Case7()
    {
        using var scope = new TaskScope.WhenAll();
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromSeconds(20), token);
            return "2";
        });
        var st3 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return 3.0;
        });
        var stFail = scope.Add(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("failed");
            return 0;
        });

        Assert.False(scope.IsScopeFaulted);

        var result = await scope.Run(
            new TaskScope.RunOptions { ThrowOnFault = false },
            TestContext.Current.CancellationToken
        );

        Assert.True(scope.IsScopeFaulted);

        Assert.Equal(ResultHandleState.Terminated, st2.GetState(result));
        Assert.Equal(ResultHandleState.Terminated, st3.GetState(result));
        Assert.Equal(ResultHandleState.Faulted, stFail.GetState(result));

        var operationException = Assert.Throws<InvalidOperationException>(
            () => stFail.ThrowIfFaulted(result)
        );
        Assert.Equal("failed", operationException.Message);
        Assert.StartsWith(
            "   at Icicle.Tests.WhenAllTests.",
            operationException.StackTrace,
            StringComparison.Ordinal
        );
        Assert.Throws<InvalidOperationException>(() => stFail.Value(result));
    }

    [Fact(DisplayName = "Invalid `RunToken` throws")]
    public async Task Case8()
    {
        using var scope1 = new TaskScope.WhenAll();
        using var scope2 = new TaskScope.WhenAll();
        var st1 = scope1.Add(async token =>
        {
            await Task.Yield();
            return "1";
        });
        var st2 = scope2.Add(async token =>
        {
            await Task.Yield();
        });

        var token1 = await scope1.Run(token: TestContext.Current.CancellationToken);
        var token2 = await scope2.Run(token: TestContext.Current.CancellationToken);

        var e = Assert.Throws<InvalidRunTokenException>(() => st1.Value(token2));
        Assert.Equal(
            "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this handle?",
            e.Message
        );
        e = Assert.Throws<InvalidRunTokenException>(() => st2.ThrowIfFaulted(token1));
        Assert.Equal(
            "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this handle?",
            e.Message
        );
    }

    [Fact(DisplayName = "`TaskScope` can be captured and added to after `Run`")]
    public async Task Case9()
    {
        using var scope = new TaskScope.WhenAll();
        var st2 = scope.Add(async _ =>
        {
            await Task.Yield();
            return scope.Add(async _ =>
            {
                await Task.Yield();
                return scope.Add(async _ =>
                {
                    await Task.Yield();
                    return "2";
                });
            });
        });

        var result = await scope.Run(
            new TaskScope.RunOptions { Timeout = TimeSpan.FromMilliseconds(15) },
            TestContext.Current.CancellationToken
        );
        Assert.Equal(ResultHandleState.Succeeded, st2.GetState(result));
        Assert.Equal(ResultHandleState.Succeeded, st2.Value(result).GetState(result));
        Assert.Equal(ResultHandleState.Succeeded, st2.Value(result).Value(result).GetState(result));
        Assert.Equal("2", st2.Value(result).Value(result).Value(result));
    }

    [Fact(DisplayName = "`TaskScope` `Run` can be given a timeout, terminating all tasks")]
    public async Task Case10()
    {
        using var scope = new TaskScope.WhenAll();
        var st1 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(30), token);
            return 1;
        });
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), token);
            return "2";
        });

        // run only for at most 15 ms
        var result = await scope.Run(
            new TaskScope.RunOptions { Timeout = TimeSpan.FromMilliseconds(15) },
            TestContext.Current.CancellationToken
        );

        // all value handles will be terminated
        Assert.Equal(ResultHandleState.Terminated, st1.GetState(result));
        Assert.Equal(ResultHandleState.Terminated, st2.GetState(result));
    }

    [Fact(DisplayName = "`Add` after `Run` throws")]
    public async Task Case11()
    {
        using var scope = new TaskScope.WhenAll();
        await scope.Run(token: TestContext.Current.CancellationToken);
        var e = Assert.Throws<TaskScopeCompletedException>(
            () =>
                scope.Add(async token =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(30), token);
                    return 1;
                })
        );
        Assert.Equal("The current `TaskScope` has already completed", e.Message);
    }

    [Fact(DisplayName = "`TaskScope` can be created for an unbounded child task context")]
    public async Task Case12()
    {
        using var scope = new TaskScope.WhenAll();
        // start a parent task
        var action = scope.Add(async _ =>
        {
            await Task.Yield();
            var sum = 0;
            // create a while loop that stops after hitting a sum
            while (sum < 10)
            {
                // create child tasks to do the summing
                scope.Add(async _ =>
                {
                    await Task.Yield();
                    Interlocked.Increment(ref sum);
                });
            }
            // we have enough sum now
            scope.Cancel();
        });
        // start the run operation in an unbounded manner
        var token = await scope.Run(
            new TaskScope.RunOptions { Bounded = false },
            TestContext.Current.CancellationToken
        );
        Assert.Equal(ResultHandleState.Succeeded, action.GetState(token));
    }

    [Fact(DisplayName = "`TaskScope` can collect all failures")]
    public async Task Case13()
    {
        using var scope = new TaskScope.WhenAll(windowSize: 2);
        var a1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(10), ct));
        var a2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            throw new InvalidOperationException();
        });
        var a3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(10), ct));
        var a4 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            throw new InvalidOperationException();
        });
        var token = await scope.Run(
            new TaskScope.RunOptions { ThrowOnFault = false, ContinueOnFault = true },
            TestContext.Current.CancellationToken
        );
        Assert.False(scope.IsScopeFaulted);
        Assert.NotEqual(ResultHandleState.Faulted, a1.GetState(token));
        Assert.Equal(ResultHandleState.Faulted, a2.GetState(token));
        Assert.Throws<InvalidOperationException>(() => a2.ThrowIfFaulted(token));
        Assert.NotEqual(ResultHandleState.Faulted, a3.GetState(token));
        Assert.Equal(ResultHandleState.Faulted, a4.GetState(token));
    }

    [Fact(DisplayName = "`TaskScope` can collect all failures while unbounded")]
    public async Task Case14()
    {
        using var scope = new TaskScope.WhenAll(windowSize: 2);
        var a1 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            scope.Cancel();
        });
        var a2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            throw new InvalidOperationException();
        });
        var a3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(10), ct));
        var a4 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            throw new InvalidOperationException();
        });
        var token = await scope.Run(
            new TaskScope.RunOptions
            {
                Bounded = false,
                ThrowOnFault = false,
                ContinueOnFault = true,
            },
            TestContext.Current.CancellationToken
        );

        Assert.False(scope.IsScopeFaulted);
        Assert.Equal(ResultHandleState.Succeeded, a1.GetState(token));
        Assert.Equal(ResultHandleState.Faulted, a2.GetState(token));
        Assert.Equal(ResultHandleState.Succeeded, a3.GetState(token));
        Assert.Equal(ResultHandleState.Faulted, a4.GetState(token));
    }

    [Fact(DisplayName = "`TaskScope` must be run before its disposed")]
    public void Case15()
    {
        var scope = new TaskScope.WhenAll();
        var e = Assert.Throws<TaskScopeNotRunException>(() => scope.Dispose());
        Assert.Equal("The current `TaskScope` has not had `Run` called on it", e.Message);
    }
}
