using ValueTaskSupplement;

namespace Icicle;

public abstract partial class TaskScope
{
    /// <summary>
    /// An implementation of a <see cref="TaskScope"/> that runs all child tasks
    /// in parallel stopping when the first child task completes
    /// </summary>
    public sealed class WhenAny : TaskScope
    {
        /// <inheritdoc />
#pragma warning disable AsyncFixer01
        protected override async ValueTask OnRun(
            IEnumerable<ValueTask> tasks,
            CancellationToken token
        )
        {
            await ValueTaskEx.WhenAny(tasks);
        }
#pragma warning restore AsyncFixer01
    }
}
