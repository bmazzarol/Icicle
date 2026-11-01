![Icicle](https://raw.githubusercontent.com/bmazzarol/Icicle/main/icicles-icon-small.png)

# Icicle

**Freeze a group of tasks into one unit of work.**

A TaskScope groups related child tasks, runs them together, and returns a ResultHandle for each promised value.

[![Nuget](https://img.shields.io/nuget/v/Icicle)](https://www.nuget.org/packages/Icicle/)

```shell
dotnet add package Icicle
```

[Getting started](https://bmazzarol.github.io/Icicle/articles/getting-started.html) - [Full documentation](https://bmazzarol.github.io/Icicle/)

## Why?

[Structured concurrency](https://en.wikipedia.org/wiki/Structured_concurrency)
treats a group of related tasks as one unit of work: if one task faults, the
whole group is cancelled. Icicle provides a `TaskScope` that coordinates child
tasks this way, drawing on
[JEP 453: Structured Concurrency](https://openjdk.org/jeps/453).

## First example

```csharp
using Icicle;

using TaskScope scope = new TaskScope.WhenAll();

ResultHandle<int> first = scope.Add(async token =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(10), token);
    return 1;
});

ResultHandle<string> second = scope.Add(async token =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(10), token);
    return "2";
});

RunToken result = await scope.Run();

int value = first.Value(result);
string text = second.Value(result);
```

Nothing runs until `Run` is called. A fault in any child task cancels the scope,
and cancellation only reaches tasks that have started.

## Scopes

- `TaskScope.WhenAll` waits for every task, optionally in windows of a given size
- `TaskScope.WhenAny` stops at the first task to finish

## What it does not do

- It is not a scheduler. Tasks run when the scope is run, not at a set time
- It does not pool or reuse threads; the scope uses ordinary `Task` execution
- It is not a general promise library. Results are readable only through the
  `RunToken` the scope returns

## See also

- [Getting started](https://bmazzarol.github.io/Icicle/articles/getting-started.html)
- [When All](https://bmazzarol.github.io/Icicle/articles/when-all.html)
- [When Any](https://bmazzarol.github.io/Icicle/articles/when-any.html)
- [Configuration](https://bmazzarol.github.io/Icicle/articles/configuration.html)
