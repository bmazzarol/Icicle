<!-- markdownlint-disable MD013 -->

# ![Icicle](https://raw.githubusercontent.com/bmazzarol/Icicle/main/icicles-icon-small.png) Icicle

<!-- markdownlint-enable MD013 -->

[![Nuget](https://img.shields.io/nuget/v/Icicle)](https://www.nuget.org/packages/Icicle/)

[Structured Concurrency](https://en.wikipedia.org/wiki/Structured_concurrency)
simplifies concurrent code by treating
groups of related tasks as a single unit of work.

Icicle provides a `TaskScope` which can coordinate a group of concurrent child
tasks as a single unit.

The design draws inspiration
from [JEP 453: Structured Concurrency](https://openjdk.org/jeps/453).

It effectively suspends (freezes :snowflake:) tasks, returning the user
a `ResultHandle` which represents the
promised value once the scope has been run.

```c#
using Icicle;

using var scope = new TaskScope.WhenAll();
// add tasks to the scope
ResultHandle<int> result1 = scope.Add(async token => {
   await Task.Delay(TimeSpan.FromMilliseconds(10), token);
   return 1; 
});
ResultHandle<string> result2 = scope.Add(async token => {
   await Task.Delay(TimeSpan.FromMilliseconds(10), token);
   return "2"; 
});
// run all tasks
RunToken result = await scope.Run();
// and access their values
int value1 = result1.Value(result);
string value2 = result2.Value(result);
```

For more details/information read the docs or have a look at the test
projects or create an issue.
