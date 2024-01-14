<!-- markdownlint-disable MD033 MD041 -->
<div align="center">

<img src="images/icicles-icon.png" alt="Icicle" width="150px"/>

# Icicle

---

[![Nuget](https://img.shields.io/nuget/v/Icicle)](https://www.nuget.org/packages/Icicle/)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=bmazzarol_Icicle&metric=coverage)](https://sonarcloud.io/summary/new_code?id=bmazzarol_Icicle)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=bmazzarol_Icicle&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=bmazzarol_Icicle)
[![CD Build](https://github.com/bmazzarol/Icicle/actions/workflows/cd-build.yml/badge.svg)](https://github.com/bmazzarol/Icicle/actions/workflows/cd-build.yml)
[![Check Markdown](https://github.com/bmazzarol/Icicle/actions/workflows/check-markdown.yml/badge.svg)](https://github.com/bmazzarol/Icicle/actions/workflows/check-markdown.yml)

:snowflake: Structured Concurrency for C# and dotnet

---

</div>
<!-- markdownlint-enable MD033 MD041 -->

## Why?

[Structured Concurrency](https://en.wikipedia.org/wiki/Structured_concurrency)
simplifies concurrent code by treating
groups of related tasks as a single unit of work.

Icicle provides a @Icicle.TaskScope which can coordinate a group of concurrent
child tasks as a single unit.

The design draws inspiration
from [JEP 453: Structured Concurrency](https://openjdk.org/jeps/453).

It effectively suspends (freezes :snowflake:) tasks, returning the user a
@Icicle.ResultHandle`1 which represents the
promised value once the scope has been run.

```c#
using Icicle;

using TaskScope scope = new TaskScope.WhenAll();
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

For more details/information keep reading the docs or have a look at the test
projects or create an issue.
