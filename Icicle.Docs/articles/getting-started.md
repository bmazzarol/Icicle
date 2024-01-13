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

```c#
// within a using block, create a scope and configure it
using var scope = new TaskScope.WhenAll(); // run all tasks at the same time
// now add tasks to the scope to run
ActionHandle t1 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
ActionHandle t2 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
ActionHandle t3 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
// now run them all; should run for around a second
RunToken token = await scope.Run();
```

Values can also be returned from the added tasks,

```c#
using var scope = new TaskScope.WhenAll();
// added tasks 
ValueHandle<string> t1 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "Hello";
});
ValueHandle<string> t2 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "World";
});
// now run them all; should run for around a second
var token = await scope.Run();
// set the value to "Hello World"
string result = $"{t1.Value(token)} {t2.Value(token)}"; 
```

The usage rules for @Icicle.TaskScope are as follows,

1. @Icicle.TaskScope.Add*
   can be called as many times as required up until
   @Icicle.TaskScope.Run* has completed (nested calls to @Icicle.TaskScope.Add*
   are supported)

   ```c#
   using var scope = new TaskScope.WhenAll();
   // keep adding
   var t1 = scope.Add(async ct => 
     await Task.Delay(TimSpan.FromSeconds(1), ct));   
   var t2 = scope.Add(async ct => 
     await Task.Delay(TimSpan.FromSeconds(1), ct));   
   var t3 = scope.Add(async ct => {
     await Task.Delay(TimSpan.FromSeconds(1), ct)
     // and nest as well
     _ = scope.Add(async ct => 
        await Task.Delay(TimSpan.FromSeconds(1), ct));
   });
   // run them
   var token = await scope.Run();
   // cannot call Add or Run from this point on 
   ```

2. @Icicle.TaskScope.Run* can only be called once

This enforces the following semantics,

* All suspended tasks are only started on @Icicle.TaskScope.Run* and
  conform to the particular execution semantics of the @Icicle.TaskScope
  instance and its configuration. For example, a `windowSize` if provided to
  @Icicle.TaskScope.WhenAll will run at most `windowSize` tasks at a time
* Any Fault will trigger cancellation
* Cancellation applies to tasks that are started, all other tasks that have not
  started never start
* Values can be accessed from @Icicle.ResultHandle`1 and
  @Icicle.ActionHandle after @Icicle.TaskScope.Run*
  
  ```c#
  using var scope = new TaskScope.WhenAll(); 
  ValueHandle<string> t1 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "Hello";
  });
  RunToken token = 
    await scope.Run(); // get token here
  string result = t1.Value(
    // pass it here
    token
  );
  ```
  