using System.Runtime.InteropServices;

namespace Icicle;

public abstract partial class TaskScope
{
    /// <summary>
    /// Options for configuring <see cref="TaskScope.Run"/>
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly record struct RunOptions
    {
        /// <summary>
        /// Options for configuring <see cref="TaskScope.Run"/>
        /// </summary>
        public RunOptions()
        {
            Timeout = default;
            ThrowOnFault = true;
            ContinueOnFault = false;
            Bounded = true;
        }

        /// <summary>
        /// Optional timeout to apply to the run
        /// </summary>
        public TimeSpan? Timeout { get; init; }

        /// <summary>
        /// Flag indicates the <see cref="Run"/> should re-throw any faults;
        /// if false it will not throw, instead leaving the exception checking to the user via the
        /// returned handlers; default is true
        /// </summary>
        public bool ThrowOnFault { get; init; }

        /// <summary>
        /// Flag indicates the <see cref="TaskScope.Run"/> should not stop on any faults
        /// and should keep running child tasks until they are all attempted
        /// </summary>
        public bool ContinueOnFault { get; init; }

        /// <summary>
        /// Flag indicates that the addition of tasks via <see cref="TaskScope.Add{T}"/>
        /// and <see cref="TaskScope.Add"/> is bounded over the life of the call to <see cref="TaskScope.Run"/>.
        /// Set this to false if the addition of tasks is unbounded over the life of <see cref="TaskScope.Run"/>.
        /// </summary>
        public bool Bounded { get; init; }
    }
}
