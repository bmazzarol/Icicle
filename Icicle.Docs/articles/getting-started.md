# Getting Started

To use this library, simply include `Icicle.dll` in your project or
grab
it from [NuGet](https://www.nuget.org/packages/Icicle/), and add
this to the top of each `.cs` file that needs it:

```C#
using Icicle;
```

Now a @Icicle.TaskScope can be created.

A `fully parallel` scope looks like this,

[!code-csharp[Example1](../../Icicle.Tests/Examples/GettingStarted.cs#Example1)]

Values can also be returned from the added tasks,

[!code-csharp[Example2](../../Icicle.Tests/Examples/GettingStarted.cs#Example2)]

The usage rules for @Icicle.TaskScope are as follows,

1. @Icicle.TaskScope.Add*
   can be called as many times as required up until
   @Icicle.TaskScope.Run* has completed (nested calls to @Icicle.TaskScope.Add*
   are supported)

   [!code-csharp[Example3](../../Icicle.Tests/Examples/GettingStarted.cs#Example3)]

2. @Icicle.TaskScope.Run* can only be called once, and must be called

This enforces the following semantics,

* All suspended tasks are only started on @Icicle.TaskScope.Run* and
  conform to the particular execution semantics of the @Icicle.TaskScope
  instance and its configuration. For example, a `windowSize` if provided to
  @Icicle.TaskScope.WhenAll will run at most `windowSize` tasks at a time
* Any Fault will trigger cancellation
* Cancellation applies to tasks that are started, all other tasks that have not
  started never start
* Values can be accessed from @Icicle.ResultHandle`1 and
  @Icicle.ResultHandle after @Icicle.TaskScope.Run*

  [!code-csharp[Example4](../../Icicle.Tests/Examples/GettingStarted.cs#Example4)]
  