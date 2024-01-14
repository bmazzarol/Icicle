namespace Icicle;

/// <summary>
/// Provides proof that <see cref="TaskScope.Run"/> was executed, which can then be exchanged for
/// the result of that execute with <see cref="ResultHandle{T}"/> or <see cref="ResultHandle"/>
/// </summary>
public sealed class RunToken
{
    internal RunToken() { }
}
