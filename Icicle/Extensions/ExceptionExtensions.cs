namespace Icicle;

internal static class ExceptionExtensions
{
    internal static Exception TryUnwrap(this Exception e)
    {
        if (e is AggregateException { InnerExceptions.Count: 1 } ae)
        {
            return ae.InnerExceptions[0];
        }

        return e;
    }

    internal static bool IsTaskCanceledException(this Exception e)
    {
        switch (e)
        {
            case TaskCanceledException:
            case AggregateException { InnerExceptions: [TaskCanceledException] }:
                return true;
            default:
                return false;
        }
    }
}
