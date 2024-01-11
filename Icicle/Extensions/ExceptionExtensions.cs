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
}
