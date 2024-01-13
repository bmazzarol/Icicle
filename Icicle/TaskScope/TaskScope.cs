using System.Collections.Concurrent;

namespace Icicle;

using Handles = ConcurrentStack<BaseHandle>;

/// <summary>
/// Core abstraction for structured concurrency.
/// <see cref="TaskScope"/> supports cases where a <see cref="Task"/> splits into several child <see cref="Task"/>,
/// and where the child <see cref="Task"/> must complete before the main <see cref="Task"/> continues.
/// A <see cref="TaskScope"/> can be used to ensure that the lifetime of a concurrent operation is confined
/// by a `using` block, just like that of a sequential operation in structured programming.
/// </summary>
public abstract partial class TaskScope : IDisposable
{
    private readonly RunToken _runToken = new();
    private readonly Handles _handles = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private int _isRunTriggered = default;

    /// <summary>
    /// Returns true if the <see cref="Run"/> has been called
    /// </summary>
    public bool IsRunTriggered => _isRunTriggered.Value();

    /// <summary>
    /// Returns true if the <see cref="Run"/> has been called and has completed
    /// </summary>
    public bool IsScopeComplete { get; private set; }

    /// <summary>
    /// Returns true if the <see cref="Run"/> has been called and has completed
    /// </summary>
    public bool IsFaulted { get; private set; }

    private void ThrowIfScopeIsComplete()
    {
        if (IsScopeComplete)
        {
            throw new InvalidOperationException(
                $"The current `{nameof(TaskScope)}` has already completed"
            );
        }
    }

    /// <summary>
    /// Adds a `func` to the scope returning a <see cref="ResultHandle{T}"/>
    /// </summary>
    /// <param name="func">function to run</param>
    /// <typeparam name="T">some T</typeparam>
    /// <returns><see cref="ResultHandle{T}"/></returns>
    public virtual ResultHandle<T> Add<T>(Func<CancellationToken, ValueTask<T>> func)
    {
        ThrowIfScopeIsComplete();
        var handle = new ResultHandle<T>(_runToken, func);
        _handles.Push(handle);
        return handle;
    }

    /// <summary>
    /// Adds a `action` to the scope returning a <see cref="ActionHandle"/>
    /// </summary>
    /// <param name="action">action to run</param>
    public virtual ActionHandle Add(Func<CancellationToken, ValueTask> action)
    {
        ThrowIfScopeIsComplete();
        var handle = new ActionHandle(_runToken, action);
        _handles.Push(handle);
        return handle;
    }

    /// <summary>
    /// Runs all added child tasks;
    /// returning a <see cref="RunToken"/> that can be used as proof to exchange for child task results
    /// </summary>
    /// <param name="timeout">optional timeout to apply to the run</param>
    /// <param name="token">cancellation token</param>
    /// <returns><see cref="RunToken"/></returns>
    /// <exception cref="InvalidOperationException">thrown if <see cref="Run"/> is called more than once</exception>
    public virtual async ValueTask<RunToken> Run(
        TimeSpan? timeout = default,
        CancellationToken token = default
    )
    {
        if (!_isRunTriggered.TrySet(value: true))
        {
            throw new InvalidOperationException(
                $"The current `{nameof(TaskScope)}` has already been `{nameof(Run)}`"
            );
        }

        _cancellationTokenSource ??= CancellationTokenSource.CreateLinkedTokenSource(
            token,
            default
        );

        if (timeout is { } time)
        {
            _cancellationTokenSource.CancelAfter(time);
        }

        try
        {
            while (!token.IsCancellationRequested && !_handles.IsEmpty)
            {
                await OnRun(
                    TaskEnumerable(_cancellationTokenSource.Token),
                    _cancellationTokenSource.Token
                );
            }
        }
        catch
        {
            IsFaulted = true;
        }
        finally
        {
            await _cancellationTokenSource.CancelAsync();
        }

        IsScopeComplete = true;
        return _runToken;
    }

    private IEnumerable<ValueTask> TaskEnumerable(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _handles.TryPop(out var handle))
        {
            yield return handle.Run(token);
        }
    }

    /// <summary>
    /// Called when a <see cref="Run"/> is requested on the <see cref="TaskScope"/>
    /// </summary>
    /// <remarks>this method is expected to raise errors</remarks>
    /// <param name="tasks">lazy <see cref="ValueTask"/>s to execute</param>
    /// <param name="token">cancellation token</param>
    /// <returns><see cref="ValueTask"/> which completes when all <see cref="ValueTask"/> are complete</returns>
    protected abstract ValueTask OnRun(IEnumerable<ValueTask> tasks, CancellationToken token);

    /// <summary>
    /// Extension point for hooking into <see cref="IDisposable.Dispose"/>
    /// </summary>
    /// <param name="disposing">flag to indicate that disposal is under way</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _handles.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
