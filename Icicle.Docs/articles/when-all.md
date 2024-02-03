# When All

The most common sort of concurrency is
to run all child tasks at the same
time. This is provided by <xref:Icicle.TaskScope.WhenAll>.

To use it,

[!code-csharp[Example1](../../Icicle.Tests/Examples/WhenAll.cs#Example1)]

## Windowing

The <xref:Icicle.TaskScope.WhenAll> supports a `windowSize` parameter
that will enforce at most `windowSize` tasks are started at
the same time,

[!code-csharp[Example2](../../Icicle.Tests/Examples/WhenAll.cs#Example2)]

Windowing is good for resource constrained shared environments,
such as a web api; It allows for control over the max
parallelism that each operation can get access to, so that
one operation does not reduces the average throughput of other
operations.
