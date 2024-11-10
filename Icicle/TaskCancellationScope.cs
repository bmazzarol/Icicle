using System.Collections.Concurrent;

namespace Icicle;

/// <summary>
/// A task cancellation scope co-ordinates the cancellation of multiple tasks when a single task is cancelled.
/// <see cref="ColdTask{T}"/> and <see cref="ColdTask"/> can be attached to a <see cref="TaskCancellationScope"/>
/// and then all registered tasks will be linked to the cancellation token of the scope.
/// </summary>
public sealed class TaskCancellationScope : CancellationTokenSource
{
    private CancellationTokenRegistration? _parentCancelRegistration;

    /// <summary>
    /// The current active task cancellation scope for the current async context
    /// </summary>
    public static AsyncLocal<TaskCancellationScope?> Current { get; } = new();

    /// <summary>
    /// The parent task cancellation scope
    /// </summary>
    public TaskCancellationScope? Parent { get; private set; }

    /// <summary>
    /// Creates a new task cancellation scope
    /// </summary>
    private TaskCancellationScope()
    {
        // get any current task cancellation scope
        var current = Current.Value;
        // set the parent to the current task cancellation scope
        Parent = current;
        // set the current task cancellation scope to this
        Current.Value = this;

        // if the parent is not null, link the cancellation token to the parent
        if (Parent is not null)
        {
            _parentCancelRegistration = Token.Register(Parent.Cancel);
        }
    }

    private static readonly ConcurrentQueue<TaskCancellationScope> Pool = new();

    /// <summary>
    /// Creates a new task cancellation scope
    /// </summary>
    /// <returns>scope</returns>
    public static TaskCancellationScope Create()
    {
        return Pool.TryDequeue(out var scope) ? scope : new TaskCancellationScope();
    }

    /// <summary>
    /// Implicitly converts the <see cref="TaskCancellationScope"/> to a <see cref="CancellationToken"/>
    /// </summary>
    /// <param name="scope">task cancellation scope</param>
    /// <returns>cancellation token</returns>
    public static implicit operator CancellationToken(TaskCancellationScope scope) => scope.Token;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Clear();
        if (TryReset())
        {
            Pool.Enqueue(this);
        }
        else
        {
            base.Dispose(disposing);
        }
    }

    private void Clear()
    {
        if (Parent is null)
        {
            Current.Value = null;
            return;
        }

        Current.Value = Parent;
        _parentCancelRegistration?.Dispose();
        _parentCancelRegistration = null;
        Parent = null;
    }
}
