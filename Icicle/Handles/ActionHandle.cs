using System.Runtime.ExceptionServices;

namespace Icicle;

/// <summary>
/// Represents a handle to a action after a <see cref="TaskScope.Run"/> from a delegate added via
/// <see cref="TaskScope.Add"/>
/// </summary>
public sealed class ActionHandle : BaseHandle
{
    private static InvalidOperationException InvalidTokenException =>
        new(
            $"Provided `token` did not match; was it returned from the same `{nameof(TaskScope)}.{nameof(TaskScope.Run)}` call that returned this `{nameof(ActionHandle)}`?"
        );

    private readonly RunToken _runToken;
    private readonly Func<CancellationToken, ValueTask> _lazyTask;
    private ValueTask? _futureAction;

    internal ActionHandle(RunToken runToken, Func<CancellationToken, ValueTask> lazyTask)
    {
        _runToken = runToken;
        _lazyTask = lazyTask;
    }

    private void ThrowOnInvalidToken(RunToken token)
    {
        if (!token.Equals(_runToken))
        {
            throw InvalidTokenException;
        }
    }

    /// <summary>
    /// Returns the current state of the <see cref="ResultHandle{T}"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <returns>current state</returns>
    public HandleState GetState(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _futureAction switch
        {
            { IsCompletedSuccessfully: true } => HandleState.Succeeded,
            { IsFaulted: true } => HandleState.Faulted,
            _ => HandleState.Terminated
        };
    }

    /// <summary>
    /// If the <see cref="ResultHandle{T}"/> has a status of <see cref="HandleState.Faulted"/> it throws the <see cref="Exception"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    /// <exception cref="Exception">any if the <see cref="ResultHandle{T}"/> has faulted</exception>
    public void ThrowIfFaulted(RunToken token)
    {
        ThrowOnInvalidToken(token);

        if (_futureAction is { IsFaulted: true } ft)
        {
            ExceptionDispatchInfo.Throw(ft.AsTask().Exception!.TryUnwrap());
        }
    }

    /// <summary>
    /// Converts the <see cref="ActionHandle"/> into a <see cref="Task"/>
    /// </summary>
    /// <param name="token">returned proof from a call to <see cref="TaskScope.Run"/></param>
    public Task AsTask(RunToken token)
    {
        ThrowOnInvalidToken(token);

        return _futureAction!.Value.AsTask();
    }

    internal override ValueTask Run(CancellationToken token)
    {
        _futureAction = _lazyTask(token);
        return _futureAction.Value;
    }
}
