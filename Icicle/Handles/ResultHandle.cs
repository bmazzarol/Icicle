#pragma warning disable S5034, S5034

using System.Runtime.ExceptionServices;

namespace Icicle;

/// <summary>
/// Represents a handle to a value produced after a <see cref="TaskScope.Run"/> from a delegate added via
/// <see cref="TaskScope.Add"/>
/// </summary>
/// <typeparam name="T">some result</typeparam>
public sealed class ResultHandle<T> : BaseHandle
{
    private readonly RunToken _runToken;
    private readonly Func<CancellationToken, ValueTask<T>> _lazyTask;
    private ValueTask<T>? _futureTask;

    internal ResultHandle(RunToken runToken, Func<CancellationToken, ValueTask<T>> lazyTask)
    {
        _runToken = runToken;
        _lazyTask = lazyTask;
    }

    private void ThrowOnInvalidToken(RunToken token)
    {
        if (!token.Equals(_runToken))
        {
            throw InvalidRunTokenException.Instance;
        }
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
    public HandleState GetState(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _futureTask switch
        {
            { IsCompletedSuccessfully: true } => HandleState.Succeeded,
            { IsFaulted: true } => HandleState.Faulted,
            _ => HandleState.Terminated
        };
    }

    /// <summary>
    /// Returns the <see cref="ResultHandle{T}"/> value against the provided proof of <see cref="TaskScope.Run"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>T or throws</returns>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    /// <exception cref="Exception">if the <see cref="ResultHandle{T}"/> has faulted</exception>
    /// <exception cref="TaskCanceledException">if the underlying <see cref="ValueTask"/> was cancelled</exception>
    public T Value(RunToken token)
    {
        ThrowOnInvalidToken(token);

        switch (_futureTask)
        {
            case { IsCompletedSuccessfully: true } st:
                return st.Result;
            case { IsFaulted: true } ft:
                ExceptionDispatchInfo.Throw(ft.AsTask().Exception!.TryUnwrap());
                return default; // unreachable
            default:
                throw new TaskCanceledException();
        }
    }

    /// <summary>
    /// Returns the <see cref="ResultHandle{T}"/> value against the provided proof of <see cref="TaskScope.Run"/>;
    /// or default
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

        if (_futureTask is { IsCompletedSuccessfully: true } st)
        {
            return st.Result;
        }

        return default;
    }

    /// <summary>
    /// If the <see cref="ResultHandle{T}"/> has a status of <see cref="HandleState.Faulted"/> it throws the <see cref="Exception"/>
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

        if (_futureTask is { IsFaulted: true } ft)
        {
            ExceptionDispatchInfo.Throw(ft.AsTask().Exception!.TryUnwrap());
        }
    }

    /// <summary>
    /// Converts the <see cref="ResultHandle{T}"/> into a <see cref="Task{T}"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="InvalidRunTokenException">
    /// if provided token is not from the same <see cref="TaskScope"/> that
    /// created this <see cref="ResultHandle{T}"/>
    /// </exception>
    public Task<T> AsTask(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _futureTask!.Value.AsTask();
    }

#pragma warning disable AsyncFixer01
    internal override async ValueTask Run(CancellationToken token)
    {
        _futureTask = _lazyTask(token);
        await _futureTask.Value;
    }
#pragma warning restore AsyncFixer01
}
