# Icicle

![Icicle](images/icicles-icon.png)

**Freeze a group of tasks into one unit of work.**

A TaskScope groups related child tasks, runs them together, and returns a ResultHandle for each promised value.

```shell
dotnet add package Icicle
```

## Why?

[Structured concurrency](https://en.wikipedia.org/wiki/Structured_concurrency)
treats a group of related tasks as one unit of work: they start together, they
finish together, and one failure cancels the rest.

Icicle models that with <xref:Icicle.TaskScope>. Tasks are added to a scope, the
scope is run once, and each task returns an <xref:Icicle.ResultHandle`1> holding
its value. The design follows
[JEP 453](https://openjdk.org/jeps/453): the tasks are frozen until the scope
runs, then their results are read from the handles.

```c#
using Icicle;

using TaskScope scope = new TaskScope.WhenAll();

ResultHandle<int> first = scope.Add(async token => {
    await Task.Delay(TimeSpan.FromMilliseconds(10), token);
    return 1;
});

ResultHandle<string> second = scope.Add(async token => {
    await Task.Delay(TimeSpan.FromMilliseconds(10), token);
    return "2";
});

RunToken result = await scope.Run();

int value1 = first.Value(result);
string value2 = second.Value(result);
```

If one of the tasks throws, the scope fails as a unit and the other tasks are
cancelled through the token.

## What it does not do

* It is not a scheduler. Tasks run when the scope is run, not at a set time
* It does not pool or reuse threads; the scope uses ordinary `Task` execution
* It is not a general promise library; results are only readable through the
  `RunToken` the scope returns

## See also

* [Getting Started](articles/getting-started.md) - run a scope
* [When All](articles/when-all.md) - the scope that waits for every task
* [When Any](articles/when-any.md) - the scope that stops at the first result
* [Configuration](articles/configuration.md) - timeouts and cancellation
