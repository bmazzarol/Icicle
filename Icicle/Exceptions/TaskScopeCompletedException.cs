#pragma warning disable RCS1194

namespace Icicle;

/// <summary>
/// Thrown when trying to modify a <see cref="TaskScope"/> after it has
/// had <see cref="TaskScope.Run"/> called
/// </summary>
public sealed class TaskScopeCompletedException : Exception
{
    private TaskScopeCompletedException()
        : base($"The current `{nameof(TaskScope)}` has already completed") { }

    internal static Exception Instance { get; } = new TaskScopeCompletedException();
}
