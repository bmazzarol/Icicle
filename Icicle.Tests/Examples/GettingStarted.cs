namespace Icicle.Tests.Examples;

public class GettingStarted
{
    [Example]
    public async Task Case1()
    {
        #region Example1

        // within a using block, create a scope and configure it
        using TaskScope scope = new TaskScope.WhenAll(); // run all tasks at the same time
        // now add tasks to the scope to run
        ResultHandle t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t2 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // now run them all; should run for around a second
        RunToken token = await scope.Run();

        #endregion
    }

    [Example]
    public async Task Case2()
    {
        #region Example2

        using TaskScope scope = new TaskScope.WhenAll();
        // added tasks
        ResultHandle<string> t1 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "Hello";
        });
        ResultHandle<string> t2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "World";
        });
        // now run them all; should run for around a second
        var token = await scope.Run();
        // set the value to "Hello World"
        $"{t1.Value(token)} {t2.Value(token)}".Should().Be("Hello World");

        #endregion
    }

    [Example]
    public async Task Case3()
    {
        #region Example3

        using TaskScope scope = new TaskScope.WhenAll();
        // keep adding
        ResultHandle t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t2 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t3 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            // and nest as well
            _ = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        });
        // run them
        RunToken token = await scope.Run();
        // cannot call Add or Run from this point on
        Assert.Throws<TaskScopeCompletedException>(
            () => scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct))
        );

        #endregion
    }

    [Example]
    public async Task Case4()
    {
        #region Example4

        using TaskScope scope = new TaskScope.WhenAll();
        ResultHandle<string> t1 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "Hello";
        });
        RunToken token = await scope.Run(); // get token here
        string result = t1.Value(
            // pass it here
            token
        );
        result.Should().Be("Hello");

        #endregion
    }
}
