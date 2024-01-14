using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

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
            throw TaskScopeCompletedException.Instance;
        }
    }

    /// <summary>
    /// Adds a `func` to the scope returning a <see cref="ResultHandle{T}"/>
    /// </summary>
    /// <param name="func">function to run</param>
    /// <typeparam name="T">some T</typeparam>
    /// <returns><see cref="ResultHandle{T}"/></returns>
    /// <exception cref="TaskScopeCompletedException">
    /// if <see cref="Run"/> has already been called on the current <see cref="TaskScope"/>
    /// </exception>
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
    /// <exception cref="TaskScopeCompletedException">
    /// if <see cref="Run"/> has already been called on the current <see cref="TaskScope"/>
    /// </exception>
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
    /// <param name="options">configuration options for <see cref="Run"/></param>
    /// <param name="token">cancellation token</param>
    /// <returns><see cref="RunToken"/></returns>
    /// <exception cref="TaskScopeCompletedException">
    /// if <see cref="Run"/> has already been called on the current <see cref="TaskScope"/>
    /// </exception>
    public virtual async ValueTask<RunToken> Run(
        RunOptions? options = default,
        CancellationToken token = default
    )
    {
        var runOptions = options ?? new RunOptions();

        if (!_isRunTriggered.TrySet(value: true))
        {
            throw TaskScopeCompletedException.Instance;
        }

        _cancellationTokenSource ??= CancellationTokenSource.CreateLinkedTokenSource(
            token,
            default
        );

        if (runOptions.Timeout is { } time)
        {
            _cancellationTokenSource.CancelAfter(time);
        }

        try
        {
            if (runOptions.Bounded)
            {
                await RunBounded(runOptions, _cancellationTokenSource.Token);
            }
            else
            {
                await RunUnbounded(runOptions, _cancellationTokenSource.Token);
            }
        }
        catch (Exception e) when (e.IsTaskCanceledException())
        {
            // swallow cancellation
        }
        catch (Exception e)
        {
            IsFaulted = true;
            if (runOptions.ThrowOnFault)
            {
                ExceptionDispatchInfo.Throw(e.TryUnwrap());
            }
        }
        finally
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                await _cancellationTokenSource.CancelAsync();
            }
        }

        IsScopeComplete = true;
        return _runToken;
    }

    private async Task RunBounded(RunOptions options, CancellationToken token)
    {
        while (!token.IsCancellationRequested && !_handles.IsEmpty)
        {
            try
            {
                await OnRun(BoundedTaskEnumerable(token), options, token);
            }
            catch when (options.ContinueOnFault)
            {
                // swallow errors and keep running
            }
        }
    }

    private IEnumerable<ValueTask> BoundedTaskEnumerable(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _handles.TryPop(out var handle))
        {
            yield return handle.Run(token);
        }
    }

    private async Task RunUnbounded(RunOptions options, CancellationToken token)
    {
        while (true)
        {
            try
            {
                await OnRun(UnboundedTaskEnumerable(token), options, token);
                return;
            }
            catch when (options.ContinueOnFault)
            {
                // swallow errors and keep running
            }
        }
    }

    private IEnumerable<ValueTask> UnboundedTaskEnumerable(CancellationToken token)
    {
        var sw = new SpinWait();
        while (!token.IsCancellationRequested)
        {
            if (_handles.TryPop(out var handle))
            {
                yield return handle.Run(token);
            }
            sw.SpinOnce();
        }
    }

    /// <summary>
    /// Called when a <see cref="Run"/> is requested on the <see cref="TaskScope"/>
    /// </summary>
    /// <remarks>this method is expected to raise errors</remarks>
    /// <param name="tasks">lazy <see cref="ValueTask"/>s to execute</param>
    /// <param name="options">run options currently applied</param>
    /// <param name="token">cancellation token</param>
    /// <returns><see cref="ValueTask"/> which completes when all <see cref="ValueTask"/> are complete</returns>
    protected abstract ValueTask OnRun(
        IEnumerable<ValueTask> tasks,
        RunOptions options,
        CancellationToken token
    );

    /// <summary>
    /// Triggers cancellation on the <see cref="TaskScope"/>
    /// </summary>
    /// <remarks>
    /// Can be used when building an unbounded <see cref="TaskScope"/> to trigger
    /// and end to the <see cref="Run"/> operation
    /// </remarks>
    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }

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
