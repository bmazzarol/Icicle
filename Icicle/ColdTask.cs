namespace Icicle;

/// <summary>
/// Represents a cold task that will not start until awaited
/// </summary>
public struct ColdTask
{
    private readonly object? _state;
    private readonly Func<object?, Task> _thunk;
    private Task? _task;

    private ColdTask(Func<object?, Task> thunk, object? state)
    {
        _thunk = thunk;
        _state = state;
        _task = null;
    }

    /// <summary>
    /// Converts the cold task to a hot task
    /// </summary>
    public Task Task
    {
        get
        {
#pragma warning disable MA0134
            Interlocked.CompareExchange(ref _task, BuildTask(), comparand: null);
#pragma warning restore MA0134
            return _task;
        }
    }

    private readonly async Task BuildTask()
    {
        try
        {
            await _thunk(_state);
        }
        catch (Exception)
        {
            if (TaskCancellationScope.Current.Value is { } scope)
            {
                scope.Cancel();
            }

            throw;
        }
    }

    /// <summary>
    /// Creates a new cold task
    /// </summary>
    /// <param name="func">The lazy task to execute</param>
    /// <returns>The cold task</returns>
    public static ColdTask New(Func<Task> func) =>
        new(static state => ((Func<Task>)state!)(), func);

    /// <summary>
    /// Creates a new cold task
    /// </summary>
    /// <param name="func">The lazy task to execute</param>
    /// <param name="state">The state to pass to the task</param>
    /// <returns>The cold task</returns>
    public static ColdTask New<TState>(Func<TState, Task> func, TState state)
        where TState : class => new((Func<object?, Task>)func, state);

    /// <summary>
    /// Creates a new cold task
    /// </summary>
    /// <param name="func">The lazy task to execute</param>
    /// <typeparam name="T">The type of the task</typeparam>
    /// <returns>The cold task</returns>
    public static ColdTask<T> New<T>(Func<Task<T>> func) =>
        new(static state => ((Func<Task<T>>)state!)(), func);

    /// <summary>
    /// Creates a new cold task
    /// </summary>
    /// <param name="func">the lazy task to execute</param>
    /// <param name="state">the state to pass to the task</param>
    /// <typeparam name="T">the type of the task</typeparam>
    /// <typeparam name="TState">the type of the state</typeparam>
    /// <returns>the cold task</returns>
    public static ColdTask<T> New<T, TState>(Func<TState, Task<T>> func, TState state)
        where TState : class => new((Func<object?, Task<T>>)func, state);
}

/// <summary>
/// Represents a cold task that will not start until awaited
/// </summary>
/// <typeparam name="T">The type of the task</typeparam>
public struct ColdTask<T>
{
    private readonly object? _state;
    private readonly Func<object?, Task<T>> _thunk;
    private Task<T>? _task;

    internal ColdTask(Func<object?, Task<T>> thunk, object? state)
    {
        _thunk = thunk;
        _state = state;
    }

    /// <summary>
    /// Converts the cold task to a hot task
    /// </summary>
    public Task<T> Task
    {
        get
        {
#pragma warning disable MA0134
            Interlocked.CompareExchange(ref _task, BuildTask(), comparand: null);
#pragma warning restore MA0134
            return _task;
        }
    }

    private readonly async Task<T> BuildTask()
    {
        try
        {
            return await _thunk(_state);
        }
        catch (Exception)
        {
            if (TaskCancellationScope.Current.Value is { } scope)
            {
                scope.Cancel();
            }

            throw;
        }
    }
}
