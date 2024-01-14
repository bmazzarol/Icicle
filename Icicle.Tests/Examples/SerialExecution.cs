namespace Icicle.Tests.Examples;

#region Example1

/// <summary>
/// Simple serial execution <see cref="TaskScope"/>. Provides the same
/// behaviour as using standard async/await
/// </summary>
public sealed class SerialExecution : TaskScope
{
    /// <inheritdoc />
    protected override async ValueTask OnRun(
        IEnumerable<ValueTask> tasks,
        RunOptions options,
        CancellationToken token
    )
    {
        foreach (var task in tasks)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            await task;
        }
    }
}

#endregion
