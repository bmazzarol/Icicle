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

        var result = await scope.Run();

        st1.ValueOrDefault(result).Should().BeNull();
        st2.ValueOrDefault(result).Should().BeNull();
        st3.ValueOrDefault(result).Should().Be("3.0");
    }
}
