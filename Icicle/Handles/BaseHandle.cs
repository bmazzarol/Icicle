namespace Icicle;

/// <summary>
/// A suspended delegate that a <see cref="TaskScope"/> can materialize
/// </summary>
public abstract class BaseHandle
{
    /// <summary>
    /// Provided <see cref="RunToken"/>
    /// </summary>
    private readonly RunToken _runToken;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="runToken"></param>
    private protected BaseHandle(RunToken runToken)
    {
        _runToken = runToken;
    }

    /// <summary>
    /// Materializes the handle
    /// </summary>
    /// <param name="token">cancellation token</param>
    /// <returns>task to await</returns>
    internal abstract ValueTask Run(CancellationToken token);

    /// <summary>
    /// Throws when the <see cref="RunToken"/> provided does not match the <see cref="_runToken"/>
    /// </summary>
    /// <param name="token">provided <see cref="RunToken"/></param>
    /// <exception cref="InvalidRunTokenException">when the <see cref="RunToken"/> do not match</exception>
    protected void ThrowOnInvalidToken(RunToken token)
    {
        if (!token.Equals(_runToken))
        {
            throw InvalidRunTokenException.Instance;
        }
    }
}
