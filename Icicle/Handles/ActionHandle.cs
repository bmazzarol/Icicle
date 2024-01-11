using System.Runtime.ExceptionServices;

namespace Icicle;

/// <summary>
/// Represents a handle to a action after a <see cref="TaskScope.Run"/> from a delegate added via
/// <see cref="TaskScope.Add"/>
/// </summary>
public sealed class ActionHandle
{
    private static InvalidOperationException InvalidTokenException =>
        new(
            $"Provided `token` did not match; was it returned from the same `{nameof(TaskScope)}.{nameof(TaskScope.Run)}` call that returned this `{nameof(ActionHandle)}`?"
        );

    private readonly RunToken _runToken;
    internal ValueTask? FutureAction;

    internal ActionHandle(RunToken runToken)
    {
        _runToken = runToken;
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

        return FutureAction switch
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

        if (FutureAction is { IsFaulted: true } ft)
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

        return FutureAction!.Value.AsTask();
    }
}
