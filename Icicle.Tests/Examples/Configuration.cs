using System.Collections.Concurrent;

namespace Icicle.Tests.Examples;

public class Configuration
{
    [Example]
    public async Task Case1()
    {
        #region Example1

        using TaskScope scope = new TaskScope.WhenAll();
        // runs for 2 seconds
        ActionHandle action = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(2), ct));
        // timeout after 1 second
        RunToken token = await scope.Run(
            new TaskScope.RunOptions { Timeout = TimeSpan.FromSeconds(1) }
        );
        // results in the action getting terminated
        action.GetState(token).Should().Be(HandleState.Terminated);

        #endregion
    }

    [Example]
    public async Task Case2()
    {
        #region Example2

        using TaskScope scope = new TaskScope.WhenAll();
        var queue = new ConcurrentQueue<string>();

        // one action adds child tasks at intervals
        ActionHandle workAction = scope.Add(async ct =>
        {
            while (!ct.IsCancellationRequested)
            {
                // child task
                scope.Add(async cct =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10), cct);
                    // do some work
                    queue.Enqueue("some work");
                });
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
            }
        });
        // second action runs for a given period of time then cancels the scope
        ActionHandle monitorAction = scope.Add(async ct =>
        {
            // wait for some work to get done
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            // now we cancel the scope; stopping any other work on the scope
            scope.Cancel();
        });

        // run it with the Bounded setting false
        // and it should run for 1 second
        RunToken token = await scope.Run(new TaskScope.RunOptions { Bounded = false });
        // both tasks are complete
        workAction.GetState(token).Should().NotBe(HandleState.Faulted);
        monitorAction.GetState(token).Should().NotBe(HandleState.Faulted);
        // we have done work
        queue.Should().NotBeEmpty().And.HaveCountGreaterOrEqualTo(10);

        #endregion
    }

    [Example]
    public async Task Case3a()
    {
        #region Example3a

        using TaskScope scope = new TaskScope.WhenAll(windowSize: 2);
        // add 4 tasks
        // completes
        ActionHandle a1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // fails
        ActionHandle a2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            throw new InvalidOperationException();
        });
        // completes
        ActionHandle a3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // fails
        ActionHandle a4 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            throw new InvalidOperationException();
        });
        // default run behaviour is to fail fast and cancel
        // so the second batch of tasks will never start
        RunToken token = await scope.Run(
            new TaskScope.RunOptions
            {
                // this leaves the error propagation to the handles
                // instead of throwing from run
                ThrowOnFault = false
            }
        );
        // only first batch of tasks has run
        a3.GetState(token).Should().Be(HandleState.Succeeded);
        a4.GetState(token).Should().Be(HandleState.Faulted);
        a1.GetState(token).Should().Be(HandleState.Terminated);
        a2.GetState(token).Should().Be(HandleState.Terminated);

        #endregion
    }

    [Example]
    public async Task Case3b()
    {
        #region Example3b

        using TaskScope scope = new TaskScope.WhenAll(windowSize: 2);
        // add 4 tasks
        // completes
        ActionHandle a1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // fails
        ActionHandle a2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            throw new InvalidOperationException();
        });
        // completes
        ActionHandle a3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // fails
        ActionHandle a4 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            throw new InvalidOperationException();
        });
        RunToken token = await scope.Run(
            new TaskScope.RunOptions
            {
                ThrowOnFault = false,
                // this will inform the scope to keep running child
                // tasks even if a batch fails
                ContinueOnFault = true
            }
        );
        // both batches of tasks have run
        a1.GetState(token).Should().Be(HandleState.Succeeded);
        a2.GetState(token).Should().Be(HandleState.Faulted);
        a3.GetState(token).Should().Be(HandleState.Succeeded);
        a4.GetState(token).Should().Be(HandleState.Faulted);

        #endregion
    }
}
