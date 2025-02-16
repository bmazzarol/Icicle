namespace Icicle.Tests.Examples;

public class WhenAll
{
    [Example]
    public async Task Case1()
    {
        #region Example1

        using TaskScope scope = new TaskScope.WhenAll(); // create the scope
        // add the child tasks
        ResultHandle t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle<string> t2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "I Love";
        });
        ResultHandle<string> t3 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "Structured Concurrency!";
        });
        // run them all here
        RunToken token = await scope.Run();
        // get back the results
        t1.ThrowIfFaulted(token);
        string result = $"{t2.Value(token)} {t3.Value(token)}";
        Assert.Equal("I Love Structured Concurrency!", result);

        #endregion
    }

    [Example]
    public async Task Case2()
    {
        #region Example2

        using TaskScope scope = new TaskScope.WhenAll(windowSize: 2);
        // gonna run these 2
        ResultHandle t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t2 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // then these 2
        ResultHandle t3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        ResultHandle t4 = scope.Add(async ct => await Task.Delay(TimeSpan.FromSeconds(1), ct));
        // run them here 2 at a time
        RunToken token = await scope.Run();
        // should be done in around 2 seconds instead of 1

        #endregion
    }
}
