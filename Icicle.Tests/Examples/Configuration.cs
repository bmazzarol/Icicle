namespace Icicle.Tests.Examples;

public class Configuration
{
    [Fact(Skip = "Example only")]
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
}
