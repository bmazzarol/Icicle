# Getting Started

Add the package, then use `Icicle` in each file that needs a scope,

```shell
dotnet add package Icicle
```

```c#
using Icicle;
```

A <xref:Icicle.TaskScope> is created with one of the `When...` factories.

A `fully parallel` scope looks like this,

[!code-csharp[Example1](../../Icicle.Tests/Examples/GettingStarted.cs#Example1)]

Values can also be returned from the added tasks,

[!code-csharp[Example2](../../Icicle.Tests/Examples/GettingStarted.cs#Example2)]

The usage rules for <xref:Icicle.TaskScope> are as follows,

1. <xref:Icicle.TaskScope.Add*>
   can be called as many times as required up until
   <xref:Icicle.TaskScope.Run*> has completed (nested calls to
   <xref:Icicle.TaskScope.Add*> are supported)

   [!code-csharp[Example3](../../Icicle.Tests/Examples/GettingStarted.cs#Example3)]

2. <xref:Icicle.TaskScope.Run*> can only be called once, and nothing runs
   until it is

The rules above give the scope these semantics,

* No child task starts until <xref:Icicle.TaskScope.Run*> is called, and each
  one then follows the execution rules of the scope and its configuration. A
  `windowSize` passed to <xref:Icicle.TaskScope.WhenAll> for example runs at
  most `windowSize` tasks at a time
* A fault in any child task cancels the scope
* Cancellation only reaches tasks that have started; tasks that have not started
  never do
* Values are read from <xref:Icicle.ResultHandle`1> and
  <xref:Icicle.ResultHandle> after <xref:Icicle.TaskScope.Run*> returns

  [!code-csharp[Example4](../../Icicle.Tests/Examples/GettingStarted.cs#Example4)]
  