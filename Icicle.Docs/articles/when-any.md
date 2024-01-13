# When Any

When you want to race 2 or more operations @Icicle.TaskScope.WhenAny
can be used,

```c#
using var scope = new TaskScope.WhenAny(); // create the scope
ValueHandle<string> t1 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(3), ct);
    return "Slow Server Result";
});
ValueHandle<string> t2 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "Average Server Result";
});
ValueHandle<string> t3 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromMilliseconds(10), ct);
    return "Fast Server Result";
});
// run all tasks, stopping when the first one completes
RunToken token = await scope.Run();
string result = 
    t1.ValueOrDefault(token) 
 ?? t2.ValueOrDefault(token)
 ?? t3.ValueOrDefault(token);
// result is "Fast Server Result" in 10 milliseconds
```
