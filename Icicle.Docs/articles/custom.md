# Build You Own TaskScope

@Icicle.TaskScope is abstract and can be implemented
to have any execution semantics that you want.

It requires an implementation of a single method,
@Icicle.TaskScope.OnRun*
which gets passed all the tasks to run.

An example of a custom task scope that runs tasks in sequence
is as follows,

```c#
public sealed class SerialExecution : TaskScope
{
    /// <inheritdoc />
    protected override async ValueTask OnRun(
        IEnumerable<ValueTask> tasks,
        CancellationToken token
    )
    {
        foreach(var task in tasks)
        {
            if(token.IsCancellationRequested)
            {
                break;
            }
            await task;
        }
    }
}
```
