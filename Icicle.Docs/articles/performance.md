# Performance

@Icicle.TaskScope performs well and has a low cost, but its not free.

It adds the following overhead,

1. A task scope is allocated, and allocates,
   1. a concurrent queue
   2. a run handle
2. Each child task allocates a handle and a place in the concurrent queue
3. A task scope run allocates,
   1. a cancellation token source
   2. an enumerator

Compared with a simple Task.WhenAll it produces the
following,

<!-- markdownlint-disable MD013 MD040 -->

```
BenchmarkDotNet v0.13.12, Ubuntu 22.04.3 LTS (Jammy Jellyfish)
Intel Core i7-4712HQ CPU 2.30GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
```

| Method           | Size     | AsyncRatio |         Mean |        Error |       StdDev |    Ratio |  RatioSD |        Gen0 |    Gen1 |     Allocated | Alloc Ratio |
|------------------|----------|------------|-------------:|-------------:|-------------:|---------:|---------:|------------:|--------:|--------------:|------------:|
| **BasicWhenAll** | **100**  | **2**      | **11.99 ms** | **0.025 ms** | **0.023 ms** | **1.00** | **0.00** |       **-** |   **-** |  **17.84 KB** |    **1.00** |
| TaskScopeWhenAll | 100      | 2          |     12.01 ms |     0.056 ms |     0.053 ms |     1.00 |     0.01 |     15.6250 |       - |       48.8 KB |        2.74 |
|                  |          |            |              |              |              |          |          |             |         |               |             |
| **BasicWhenAll** | **100**  | **4**      | **11.98 ms** | **0.023 ms** | **0.021 ms** | **1.00** | **0.00** |       **-** |   **-** |   **10.8 KB** |    **1.00** |
| TaskScopeWhenAll | 100      | 4          |     12.00 ms |     0.025 ms |     0.023 ms |     1.00 |     0.00 |           - |       - |      33.96 KB |        3.14 |
|                  |          |            |              |              |              |          |          |             |         |               |             |
| **BasicWhenAll** | **1000** | **2**      | **12.08 ms** | **0.085 ms** | **0.076 ms** | **1.00** | **0.00** | **46.8750** |   **-** | **169.02 KB** |    **1.00** |
| TaskScopeWhenAll | 1000     | 2          |     12.45 ms |     0.185 ms |     0.164 ms |     1.03 |     0.02 |    109.3750 | 46.8750 |     477.71 KB |        2.83 |
|                  |          |            |              |              |              |          |          |             |         |               |             |
| **BasicWhenAll** | **1000** | **4**      | **12.01 ms** | **0.031 ms** | **0.028 ms** | **1.00** | **0.00** | **31.2500** |   **-** |   **98.7 KB** |    **1.00** |
| TaskScopeWhenAll | 1000     | 4          |     12.11 ms |     0.079 ms |     0.074 ms |     1.01 |     0.01 |     93.7500 | 31.2500 |     329.27 KB |        3.34 |

<!-- markdownlint-enable MD013 MD040 -->

For the features it provides I think the cost is acceptable.
However if you are writing high performance code, or need this behaviour on a
hot path, a crafted solution might be better.

It's also very closure and delegate heavy for the caller, this is currently
baked into the design and is not avoidable.

I'm open to any and all performance improvement suggestions to reduce the cost
of using this abstraction.
