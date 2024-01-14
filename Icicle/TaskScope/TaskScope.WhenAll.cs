using ValueTaskSupplement;

namespace Icicle;

public abstract partial class TaskScope
{
    /// <summary>
    /// An implementation of a <see cref="TaskScope"/> that runs all child tasks
    /// in parallel with optional windowing based on a provided `windowSize`
    /// </summary>
    public sealed class WhenAll : TaskScope
    {
        private readonly int? _windowSize;

        /// <summary>
        /// Constructs a new <see cref="TaskScope.WhenAll"/>
        /// </summary>
        /// <param name="windowSize">optional window size</param>
        public WhenAll(int? windowSize = default)
        {
            _windowSize = windowSize;
        }

        /// <inheritdoc />
        protected override async ValueTask OnRun(
            IEnumerable<ValueTask> tasks,
            RunOptions options,
            CancellationToken token
        )
        {
            if (_windowSize is not { } size)
            {
                await ValueTaskEx.WhenAll(tasks);
            }
            else
            {
                foreach (var window in tasks.Chunk(size))
                {
                    await ValueTaskEx.WhenAll(window);
                }
            }
        }
    }
}
