namespace Icicle;

/// <summary>
/// States a <see cref="ResultHandle{T}"/> and <see cref="ResultHandle"/> can be in
/// </summary>
public enum ResultHandleState
{
    /// <summary>
    /// The operation completed successfully
    /// </summary>
    Succeeded,

    /// <summary>
    /// The operation completed with an error
    /// </summary>
    Faulted,

    /// <summary>
    /// The operation completed due to cancellation/forced termination
    /// </summary>
    /// <remarks>
    /// If a <see cref="ResultHandle{T}"/> or <see cref="ResultHandle"/> are not run
    /// and the <see cref="TaskScope.Run"/> is cancelled before they are attempted,
    /// they will also have this value; indicating that
    /// the <see cref="TaskScope"/> will never run them.
    /// </remarks>
    Terminated
}
