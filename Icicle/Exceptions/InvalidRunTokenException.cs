#pragma warning disable RCS1194

namespace Icicle;

/// <summary>
/// Thrown when a <see cref="RunToken"/> passed to a handle did not match
/// the handle's <see cref="RunToken"/>; ie. the <see cref="TaskScope"/> that produced
/// the <see cref="RunToken"/> was not the one that the handle was produced from
/// </summary>
public sealed class InvalidRunTokenException : Exception
{
    private InvalidRunTokenException()
        : base(
            $"Provided `token` did not match; was it returned from the same `{nameof(TaskScope)}.{nameof(TaskScope.Run)}` call that returned this handle?"
        ) { }

    internal static Exception Instance { get; } = new InvalidRunTokenException();
}
