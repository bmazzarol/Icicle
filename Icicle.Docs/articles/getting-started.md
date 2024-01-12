# Getting Started

To use this library, simply include `Icicle.dll` in your project or
grab
it from [NuGet](https://www.nuget.org/packages/Icicle/), and add
this to the top of each `.cs` file that needs it:

```C#
using Icicle;
```

Now a @Icicle.TaskScope can be created.

A simple WhenAll scope looks like this,

```c#
// within a using block, create a scope and configure it
using var scope = new TaskScope.WhenAll();
// now add tasks to the scope to run
ActionHandle t1 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
ActionHandle t2 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
ActionHandle t3 = scope.Add(async ct => 
    await Task.Delay(TimSpan.FromSeconds(1), ct));
// now run them all; should run for around a second
RunToken token = scope.Run();
// the value can be checked for errors
t1.ThrowIfFaulted(token);
t2.ThrowIfFaulted(token);
t3.ThrowIfFaulted(token);
```

Values can also be returned from the child tasks,

```c#
using var scope = new TaskScope.WhenAll();
// now add tasks to the scope to run
ValueHandle<string> t1 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "Hello";
});
ValueHandle<string> t2 = scope.Add(async ct => {
    await Task.Delay(TimSpan.FromSeconds(1), ct);
    return "World";
});
// now run them all; should run for around a second
var token = scope.Run();
// set the value to "Hello World"
string result = $"{t1.Value(token)} {t2.Value(token)}"; 
```
