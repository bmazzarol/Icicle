namespace Icicle.Tests.Examples;

public class WhenAny
{
    [Example]
    public async Task Case1()
    {
        #region Example1

        using TaskScope scope = new TaskScope.WhenAny(); // create the scope
        ResultHandle<string> t1 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
            return "Slow Server Result";
        });
        ResultHandle<string> t2 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return "Average Server Result";
        });
        ResultHandle<string> t3 = scope.Add(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
            return "Fast Server Result";
        });
        // run all tasks, stopping when the first one completes
        RunToken token = await scope.Run();
        string? result =
            t1.ValueOrDefault(token) ?? t2.ValueOrDefault(token) ?? t3.ValueOrDefault(token);
        result.Should().Be("Fast Server Result");

        #endregion
    }
}
