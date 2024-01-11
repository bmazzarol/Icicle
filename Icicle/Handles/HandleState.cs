namespace Icicle;

/// <summary>
/// States a <see cref="ResultHandle{T}"/> can be in
/// </summary>
public enum HandleState
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
    Terminated
}
