# Configuration Options

@Icicle.TaskScope.Run* can be configured via @Icicle.TaskScope.RunOptions
for a number of scenarios.

## Timeout

@Icicle.TaskScope.RunOptions.Timeout can be used to pass a timeout
that will stop the running scope if it is reached before the scopes child tasks
complete.

[!code-csharp[Example1](../../Icicle.Tests/Examples/Configuration.cs#Example1)]

## Error Propagation

There are 2 main options that control how faults/errors are handled by
@Icicle.TaskScope.Run*,

1. @Icicle.TaskScope.RunOptions.ThrowOnFault _(default: `true`)_ - should run
   throw or delegate error checking to the returned handles _(it then becomes
   the users problem to ensure all handles are checked)_
2. @Icicle.TaskScope.RunOptions.ContinueOnFault _(default: `false`)_ - should
   run keep attempting child tasks or stop on the first failure

An example of the default behaviour,

[!code-csharp[Example3a](../../Icicle.Tests/Examples/Configuration.cs#Example3a)]

However you might want to just run all child tasks regardless of failures and
deal with all failures at the end, for example,

[!code-csharp[Example3b](../../Icicle.Tests/Examples/Configuration.cs#Example3b)]

## Unbounded Task Scope

The default @Icicle.TaskScope.Run* behaviour is to run all child tasks that
have been added since scope creation and any that get added while its running,
as long as all the initial child tasks complete.

If the initial child tasks are potentially infinite and adding their own
child tasks to the scope, it creates a requirement for the task scope to handle
unbounded tasks. This can be done by setting
@Icicle.TaskScope.RunOptions.Bounded to `false`.

[!code-csharp[Example2](../../Icicle.Tests/Examples/Configuration.cs#Example2)]
