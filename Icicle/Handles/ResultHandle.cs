#pragma warning disable S5034, S5034

using System.Runtime.ExceptionServices;

namespace Icicle;

/// <summary>
/// Represents a handle to an action after a <see cref="TaskScope.Run"/> from a delegate added via
/// <see cref="TaskScope.Add"/>
/// </summary>
public sealed class ResultHandle : BaseHandle
{
    private readonly Func<CancellationToken, ValueTask> _taskFactory;
    private ValueTask? _task;

    internal ResultHandle(RunToken runToken, Func<CancellationToken, ValueTask> taskFactory)
        : base(runToken)
    {
        _taskFactory = taskFactory;
    }

    /// <summary>
    /// Returns the current state of the <see cref="ResultHandle"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>current state</returns>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle"/>
    /// </exception>
    public ResultHandleState GetState(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _task switch
        {
            { IsCompletedSuccessfully: true } => ResultHandleState.Succeeded,
            { IsFaulted: true } => ResultHandleState.Faulted,
            _ => ResultHandleState.Terminated
        };
    }

    /// <summary>
    /// If the <see cref="ResultHandle"/> has a status of <see cref="ResultHandleState.Faulted"/> it throws the <see cref="Exception"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle"/>
    /// </exception>
    /// <exception cref="Exception">if the <see cref="ResultHandle"/> has faulted</exception>
    public void ThrowIfFaulted(RunToken token)
    {
        ThrowOnInvalidToken(token);

        if (_task is { IsFaulted: true } ft)
        {
            ExceptionDispatchInfo.Capture(ft.AsTask().Exception!.TryUnwrap()).Throw();
        }
    }

    /// <summary>
    /// Casts the <see cref="ResultHandle"/> as a <see cref="ValueTask"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle"/>
    /// </exception>
    public ValueTask AsValueTask(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _task!.Value;
    }

    /// <inheritdoc />
    internal override ValueTask Run(CancellationToken token)
    {
        _task = _taskFactory(token);
        return _task.Value;
    }
}

/// <summary>
/// Represents a handle to a result produced after a <see cref="TaskScope.Run"/> from a delegate added via
/// <see cref="TaskScope.Add{T}"/>
/// </summary>
/// <typeparam name="T">some result</typeparam>
public sealed class ResultHandle<T> : BaseHandle
{
    private readonly Func<CancellationToken, ValueTask<T>> _taskFactory;
    private ValueTask<T>? _task;

    internal ResultHandle(RunToken runToken, Func<CancellationToken, ValueTask<T>> taskFactory)
        : base(runToken)
    {
        _taskFactory = taskFactory;
    }

    /// <summary>
    /// Returns the current state of the <see cref="ResultHandle{T}"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>current state</returns>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    public ResultHandleState GetState(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _task switch
        {
            { IsCompletedSuccessfully: true } => ResultHandleState.Succeeded,
            { IsFaulted: true } => ResultHandleState.Faulted,
            _ => ResultHandleState.Terminated
        };
    }

    /// <summary>
    /// Returns the <see cref="ResultHandle{T}"/> value
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>T or throws</returns>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    /// <exception cref="Exception">if the <see cref="ResultHandle{T}"/> has faulted</exception>
    /// <exception cref="TaskCanceledException">if the <see cref="ResultHandle{T}"/> was <see cref="ResultHandleState.Terminated"/></exception>
    public T Value(RunToken token)
    {
        ThrowOnInvalidToken(token);

        switch (_task)
        {
            case { IsCompletedSuccessfully: true } st:
                return st.Result;
            case { IsFaulted: true } ft:
                ExceptionDispatchInfo.Capture(ft.AsTask().Exception!.TryUnwrap()).Throw();
                return default!; // unreachable
            default:
                throw new TaskCanceledException();
        }
    }

    /// <summary>
    /// Returns the <see cref="ResultHandle{T}"/> value or default
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>T or default</returns>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    /// <exception cref="Exception">if the <see cref="ResultHandle{T}"/> has faulted</exception>
    public T? ValueOrDefault(RunToken token)
    {
        ThrowOnInvalidToken(token);

        ThrowIfFaulted(token);

        if (_task is { IsCompletedSuccessfully: true } st)
        {
            return st.Result;
        }

        return default;
    }

    /// <summary>
    /// If the <see cref="ResultHandle{T}"/> has a status of <see cref="ResultHandleState.Faulted"/> it throws the <see cref="Exception"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    /// <exception cref="Exception">if the <see cref="ResultHandle{T}"/> has faulted</exception>
    public void ThrowIfFaulted(RunToken token)
    {
        ThrowOnInvalidToken(token);

        if (_task is { IsFaulted: true } ft)
        {
            ExceptionDispatchInfo.Capture(ft.AsTask().Exception!.TryUnwrap()).Throw();
        }
    }

    /// <summary>
    /// Casts the <see cref="ResultHandle{T}"/> as a <see cref="ValueTask{T}"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    public ValueTask<T> AsValueTask(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _task!.Value;
    }

#pragma warning disable AsyncFixer01
    internal override async ValueTask Run(CancellationToken token)
    {
        _task = _taskFactory(token);
        await _task.Value;
    }
#pragma warning restore AsyncFixer01
}
