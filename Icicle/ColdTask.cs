using System.Runtime.CompilerServices;

namespace Icicle;

/// <summary>
/// Represents a cold task that will not start until awaited
/// </summary>
public struct ColdTask
{
    private readonly Func<Task> _thunk;
    private Task? _task;

    private ColdTask(Func<Task> thunk)
    {
        _thunk = thunk;
        _task = null;
    }

    /// <summary>
    /// Converts the cold task to a hot task
    /// </summary>
    public Task Task
    {
        get
        {
            if (_task is not null)
            {
                return _task;
            }

#pragma warning disable MA0134
            Interlocked.CompareExchange(ref _task, BuildTask(), comparand: null);
#pragma warning restore MA0134
            return _task;
        }
    }

    /// <summary>
    /// The awaiter for the task
    /// </summary>
    /// <returns>task awaiter</returns>
    public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

    private readonly async Task BuildTask()
    {
        try
        {
            await _thunk();
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
    public static ColdTask New(Func<Task> func) => new(func);

    /// <summary>
    /// Creates a new cold task
    /// </summary>
    /// <param name="func">The lazy task to execute</param>
    /// <typeparam name="T">The type of the task</typeparam>
    /// <returns>The cold task</returns>
    public static ColdTask<T> New<T>(Func<Task<T>> func) => new(func);
}

/// <summary>
/// Represents a cold task that will not start until awaited
/// </summary>
/// <typeparam name="T">The type of the task</typeparam>
public struct ColdTask<T>
{
    private readonly Func<Task<T>> _thunk;
    private Task<T>? _task;

    internal ColdTask(Func<Task<T>> thunk)
    {
        _thunk = thunk;
    }

    /// <summary>
    /// Converts the cold task to a hot task
    /// </summary>
    public Task<T> Task
    {
        get
        {
            if (_task is not null)
            {
                return _task;
            }

#pragma warning disable MA0134
            Interlocked.CompareExchange(ref _task, BuildTask(), comparand: null);
#pragma warning restore MA0134
            return _task;
        }
    }

    /// <summary>
    /// The awaiter for the task
    /// </summary>
    /// <returns>task awaiter</returns>
    public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

    private readonly async Task<T> BuildTask()
    {
        try
        {
            return await _thunk();
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
