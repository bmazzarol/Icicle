namespace Icicle;

/// <summary>
/// Provides a proof that <see cref="TaskScope.Run"/> was done, this can be exchanged for
/// values within <see cref="ResultHandle{T}"/> or <see cref="ActionHandle"/> via calls to their value methods
/// </summary>
public sealed class RunToken
{
    internal RunToken() { }
}
