using System;

namespace Avalonia
{
    /// <summary>
    /// Thrown when an unexpected internal error occurs within Avalonia framework code, indicating
    /// a bug in Avalonia itself rather than user code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception indicates that Avalonia has encountered an invalid state or logic path that
    /// should be impossible under normal circumstances. If you encounter this exception, it suggests
    /// a bug in the framework rather than incorrect usage.
    /// </para>
    /// <para>
    /// When this exception is thrown, please report it to the Avalonia team with a minimal reproduction
    /// case at https://github.com/AvaloniaUI/Avalonia/issues.
    /// </para>
    /// </remarks>
    public class AvaloniaInternalException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaInternalException"/> class with
        /// a description of the internal error.
        /// </summary>
        /// <param name="message">
        /// A message describing the internal error condition. Should include enough context to help
        /// diagnose the issue.
        /// </param>
        public AvaloniaInternalException(string message)
            : base(message)
        {
        }
    }
}
