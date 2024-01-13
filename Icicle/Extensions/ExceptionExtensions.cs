namespace Icicle;

internal static class ExceptionExtensions
{
    internal static Exception TryUnwrap(this Exception e)
    {
        if (e is not AggregateException ae)
        {
            return e;
        }

        if (ae.InnerExceptions.Count == 1)
        {
            return ae.InnerExceptions[0];
        }

        return ae;
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
