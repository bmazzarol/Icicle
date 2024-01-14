namespace Icicle.Tests;

public static class ExceptionExtensionsTests
{
    [Fact(DisplayName = "AggregateExceptions with multiple child exceptions are not unwrapped")]
    public static async Task Case1()
    {
        using var scope = new TaskScope.WhenAll();
        var ae = new AggregateException(
            [new InvalidOperationException(), new InvalidOperationException()]
        );
        scope.Add(async _ =>
        {
            await Task.Yield();
            throw ae;
        });
        var e = await Assert.ThrowsAsync<AggregateException>(async () => await scope.Run());
        e.InnerExceptions.Should().HaveCount(2);
    }
}
