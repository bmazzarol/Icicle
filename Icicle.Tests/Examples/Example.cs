namespace Icicle.Tests.Examples;

[AttributeUsage(AttributeTargets.Method)]
public class ExampleAttribute : FactAttribute
{
    public ExampleAttribute()
    {
        var shouldRun = bool.Parse(Environment.GetEnvironmentVariable("RUN_EXAMPLES") ?? "false");

        if (!shouldRun)
        {
            Skip = "Example only";
        }
    }
}
