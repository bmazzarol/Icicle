namespace Icicle.Tests;

public class WhenAnyTests
{
    [Fact(DisplayName = "Many `Add` operations can be done and first to complete wins")]
    public async Task Case1()
    {
        using var scope = new TaskScope.WhenAny();
        var st1 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), token);
            return "1";
        });
        var st2 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), token);
            return "2";
        });
        var st3 = scope.Add(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(5), token);
            return "3.0";
        });

        var token = await scope.Run(token: TestContext.Current.CancellationToken);

        Assert.Null(st1.ValueOrDefault(token));
        Assert.Null(st2.ValueOrDefault(token));
        Assert.Equal("3.0", st3.ValueOrDefault(token));
    }

    [Fact(DisplayName = "Many `Add` actions can be done and first to complete wins")]
    public async Task Case2()
    {
        using var scope = new TaskScope.WhenAny();
        var t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(100), ct));
        var t2 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(1000), ct));
        var t3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(2000), ct));
        var t4 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(1), ct));
        var token = await scope.Run(token: TestContext.Current.CancellationToken);

        Assert.Equal(ResultHandleState.Terminated, t1.GetState(token));
        Assert.Equal(ResultHandleState.Terminated, t2.GetState(token));
        Assert.Equal(ResultHandleState.Terminated, t3.GetState(token));
        Assert.Equal(ResultHandleState.Succeeded, t4.GetState(token));
    }
}
