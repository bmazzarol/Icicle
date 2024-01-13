# When All

Most common sort of concurrency requirement is run all child tasks at the same
time. This is provided by @Icicle.TaskScope.WhenAll.

To use it,

```c#
using var scope = new TaskScope.WhenAll(); // create the scope
// add the child tasks
ActionHandle t1 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
ValueHandle<string> t2 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "I Love";
});
ValueHandle<string> t3 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "Structured Concurrency!";
});
// run them all here
RunToken token = await scope.Run();
// get back the results
t1.ThrowIfFaulted(token);
string result =  $"{t2.Value(token)} {t3.Value(token)}";
// "I Love Structured Concurrency!"
```

## Windowing

The @Icicle.TaskScope.WhenAll supports a `windowSize` parameter
that will enforce at most `windowSize` tasks are started at
the same time,

```c#
using var scope = new TaskScope.WhenAll(windowSize: 2);
// gonna run these 2
var t1 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
var t2 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
// then these 2
var t3 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
var t4 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
// run them here 2 at a time
RunToken token = await scope.Run();
// should be done in around 2 seconds instead of 1
```

Windowing is good for resource constrained shared environments,
such as a web api; It allows for control over the max
parallelism that each operation can get access to, so that
one operation does not reduces the average throughput of other
operations.
