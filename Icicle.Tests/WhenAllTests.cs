using static Xunit.Assert;

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

        st1.GetState(result).Should().Be(HandleState.Succeeded);
        st4.GetState(result).Should().Be(HandleState.Succeeded);

        st1.Value(result).Should().Be(1);
        (await st1.AsTask(result)).Should().Be(1);
        st2.Value(result).Should().Be("2");
        st3.Value(result).Should().Be(3.0);
        await st4.AsTask(result);
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
        var exception = await ThrowsAsync<InvalidOperationException>(async () => await scope.Run());
        exception.Message.Should().Be("The current `TaskScope` has already been `Run`");
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

        st1.GetState(result).Should().Be(HandleState.Terminated);
        Throws<TaskCanceledException>(() => st1.Value(result));
        st2.GetState(result).Should().Be(HandleState.Terminated);
        st3.GetState(result).Should().Be(HandleState.Terminated);
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

        scope.IsFaulted.Should().BeFalse();

        var result = await scope.Run();

        scope.IsFaulted.Should().BeTrue();

        st2.GetState(result).Should().Be(HandleState.Terminated);
        st3.GetState(result).Should().Be(HandleState.Terminated);
        stFail.GetState(result).Should().Be(HandleState.Faulted);

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

        Throws<InvalidOperationException>(() => st1.Value(token2))
            .Message.Should()
            .Be(
                "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this `ResultHandle`?"
            );
        Throws<InvalidOperationException>(() => st2.ThrowIfFaulted(token1))
            .Message.Should()
            .Be(
                "Provided `token` did not match; was it returned from the same `TaskScope.Run` call that returned this `ActionHandle`?"
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

        var result = await scope.Run(timeout: TimeSpan.FromMilliseconds(15));
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
        var result = await scope.Run(timeout: TimeSpan.FromMilliseconds(15));

        // all value handles will be terminated
        st1.GetState(result).Should().Be(HandleState.Terminated);
        st2.GetState(result).Should().Be(HandleState.Terminated);
    }

    [Fact(DisplayName = "`Add` after `Run` throws")]
    public async Task Case11()
    {
        using var scope = new TaskScope.WhenAll();
        await scope.Run();
        Throws<InvalidOperationException>(
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
}
