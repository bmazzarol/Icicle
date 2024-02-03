# Build You Own TaskScope

<xref:Icicle.TaskScope> is abstract and can be implemented
to have any execution semantics that you want.

It requires an implementation of a single method,
<xref:Icicle.TaskScope.OnRun*>
which gets passed all the tasks to run.

An example of a custom task scope that runs tasks in sequence
is as follows,

[!code-csharp[Example1](../../Icicle.Tests/Examples/SerialExecution.cs#Example1)]
