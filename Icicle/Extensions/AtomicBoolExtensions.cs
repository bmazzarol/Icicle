namespace Icicle;

internal static class AtomicBoolExtensions
{
    internal static bool TrySet(this ref int bit, bool value)
    {
        return value
            ? Interlocked.CompareExchange(ref bit, 1, 0) == 0
            : Interlocked.CompareExchange(ref bit, 0, 1) == 1;
    }

    internal static bool Value(this ref int bit) => Interlocked.CompareExchange(ref bit, 1, 1) == 1;
}
