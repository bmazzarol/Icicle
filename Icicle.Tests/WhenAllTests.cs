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

        var result = await scope.Run();

        st1.ThrowIfFaulted(result);
        st2.ThrowIfFaulted(result);
        st3.ThrowIfFaulted(result);
        st4.ThrowIfFaulted(result);

        st1.GetState(result).Should().Be(ResultHandleState.Succeeded);
        st4.GetState(result).Should().Be(ResultHandleState.Succeeded);

        st1.Value(result).Should().Be(1);
        (await st1.AsValueTask(result)).Should().Be(1);
        st2.Value(result).Should().Be("2");
        st3.Value(result).Should().Be(3.0);
        await st4.AsValueTask(result);
    }

    [Fact(DisplayName = "`Run` with no `Add` calls is a no-op")]
    public async Task Case2()
    {
        using var scope = new TaskScope.WhenAll();
        var result = await scope.Run();
        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "`Run` can only be called once")]
    public async Task Case3()
    {
        using var scope = new TaskScope.WhenAll();
        scope.IsRunTriggered.Should().BeFalse();
        await scope.Run();
        scope.IsRunTriggered.Should().BeTrue();
        var exception = await ThrowsAsync<TaskScopeCompletedException>(
            async () => await scope.Run()
        );
        exception.Message.Should().Be("The current `TaskScope` has already completed");
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

        var result = await scope.Run();

        st1.Value(result).Should().Be(1);
        st2.Value(result).Should().Be("2");
        st3.Value(result).Should().Be(3.0);
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

        var cts = new CancellationTokenSource();
        cts.Cancel();
        var result = await scope.Run(token: cts.Token);

        st1.GetState(result).Should().Be(ResultHandleState.Terminated);
        Throws<TaskCanceledException>(() => st1.Value(result));
        st2.GetState(result).Should().Be(ResultHandleState.Terminated);
        st3.GetState(result).Should().Be(ResultHandleState.Terminated);
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

        scope.IsScopeFaulted.Should().BeFalse();

        var result = await scope.Run(new TaskScope.RunOptions { ThrowOnFault = false });

        scope.IsScopeFaulted.Should().BeTrue();

        st2.GetState(result).Should().Be(ResultHandleState.Terminated);
        st3.GetState(result).Should().Be(ResultHandleState.Terminated);
        stFail.GetState(result).Should().Be(ResultHandleState.Faulted);

        var operationException = Throws<InvalidOperationException>(
            () => stFail.ThrowIfFaulted(result)
        );
        operationException.Message.Should().Be("failed");
        operationException.StackTrace.Should().StartWith("   at Icicle.Tests.WhenAllTests.");

        Throws<InvalidOperationException>(() => stFail.Value(result));
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

        var token1 = await scope1.Run();
        var token2 = await scope2.Run();

        Throws<InvalidRunTokenException>(() => st1.Value(token2))
            .Message.Should()
            .Be(
                "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this handle?"
            );
        Throws<InvalidRunTokenException>(() => st2.ThrowIfFaulted(token1))
            .Message.Should()
            .Be(
                "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this handle?"
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
            new TaskScope.RunOptions { Timeout = TimeSpan.FromMilliseconds(15) }
        );
        st2.Value(result).Value(result).Value(result).Should().Be("2");
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
            new TaskScope.RunOptions { Timeout = TimeSpan.FromMilliseconds(15) }
        );

        // all value handles will be terminated
        st1.GetState(result).Should().Be(ResultHandleState.Terminated);
        st2.GetState(result).Should().Be(ResultHandleState.Terminated);
    }

    [Fact(DisplayName = "`Add` after `Run` throws")]
    public async Task Case11()
    {
        using var scope = new TaskScope.WhenAll();
        await scope.Run();
        Throws<TaskScopeCompletedException>(
            () =>
                scope.Add(async token =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(30), token);
                    return 1;
                })
        )
            .Message.Should()
            .Be("The current `TaskScope` has already completed");
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
        var token = await scope.Run(new TaskScope.RunOptions { Bounded = false });
        action.GetState(token).Should().Be(ResultHandleState.Succeeded);
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
            new TaskScope.RunOptions { ThrowOnFault = false, ContinueOnFault = true }
        );
        a1.GetState(token).Should().NotBe(ResultHandleState.Faulted);
        a2.GetState(token).Should().Be(ResultHandleState.Faulted);
        Throws<InvalidOperationException>(() => a2.ThrowIfFaulted(token));
        a3.GetState(token).Should().NotBe(ResultHandleState.Faulted);
        a4.GetState(token).Should().Be(ResultHandleState.Faulted);
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
                ContinueOnFault = true
            }
        );
        a1.GetState(token).Should().Be(ResultHandleState.Succeeded);
        a2.GetState(token).Should().Be(ResultHandleState.Faulted);
        a3.GetState(token).Should().Be(ResultHandleState.Succeeded);
        a4.GetState(token).Should().Be(ResultHandleState.Faulted);
    }

    [Fact(DisplayName = "`TaskScope` must be run before its disposed")]
    public void Case15()
    {
        var scope = new TaskScope.WhenAll();
        Throws<TaskScopeNotRunException>(() => scope.Dispose())
            .Message.Should()
            .Be("The current `TaskScope` has not had `Run` called on it");
    }
}
