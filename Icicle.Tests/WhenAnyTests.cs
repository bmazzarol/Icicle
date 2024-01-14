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

        var token = await scope.Run();

        st1.ValueOrDefault(token).Should().BeNull();
        st2.ValueOrDefault(token).Should().BeNull();
        st3.ValueOrDefault(token).Should().Be("3.0");
    }

    [Fact(DisplayName = "Many `Add` actions can be done and first to complete wins")]
    public async Task Case2()
    {
        using var scope = new TaskScope.WhenAny();
        var t1 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(10), ct));
        var t2 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(100), ct));
        var t3 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(200), ct));
        var t4 = scope.Add(async ct => await Task.Delay(TimeSpan.FromMilliseconds(1), ct));
        var token = await scope.Run();
        t1.GetState(token).Should().Be(HandleState.Terminated);
        t2.GetState(token).Should().Be(HandleState.Terminated);
        t3.GetState(token).Should().Be(HandleState.Terminated);
        t4.GetState(token).Should().Be(HandleState.Succeeded);
    }
}
