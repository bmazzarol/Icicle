namespace Icicle;

/// <summary>
/// Base handle to a future value
/// </summary>
public abstract class BaseHandle
{
    /// <summary>
    /// Materializes the handle
    /// </summary>
    /// <param name="token">cancellation token</param>
    /// <returns>task to await</returns>
    internal abstract ValueTask Run(CancellationToken token);
}
