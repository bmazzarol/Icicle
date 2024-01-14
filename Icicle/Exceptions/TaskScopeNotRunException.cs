#pragma warning disable RCS1194

namespace Icicle;

/// <summary>
/// Thrown when a <see cref="IDisposable.Dispose"/> is called on a <see cref="TaskScope"/>
/// and a <see cref="TaskScope.Run"/> has not been called on it
/// </summary>
public sealed class TaskScopeNotRunException : Exception
{
    private TaskScopeNotRunException()
        : base(
            $"The current `{nameof(TaskScope)}` has not had `{nameof(TaskScope.Run)}` called on it"
        ) { }

    internal static Exception Instance { get; } = new TaskScopeNotRunException();
}
