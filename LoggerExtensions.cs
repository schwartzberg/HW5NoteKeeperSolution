using System.Runtime.CompilerServices;

namespace HW5NoteKeeperSolution
{
    /// <summary>
    /// Provides small logging helpers that enrich log messages with caller metadata.
    /// </summary>
    /// <remarks>
    /// These helpers keep the call sites in <c>Program.cs</c> readable while still including
    /// the file, line number, and member name in the emitted log entry. That is useful for this
    /// demo because the startup path performs several important steps in a compact file.
    /// </remarks>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Writes an informational log message and attaches caller information automatically.
        /// </summary>
        /// <param name="logger">The logger that writes the message.</param>
        /// <param name="message">The human-readable message to log.</param>
        /// <param name="caller">The member name provided by the compiler.</param>
        /// <param name="file">The source file path provided by the compiler.</param>
        /// <param name="lineNumber">The source line number provided by the compiler.</param>
        public static void LogInformationWithCallerInfo(this ILogger logger, string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            logger.LogInformation("{File}:{Line} [{Caller}] - {Message}", file, lineNumber, caller, message);
        }

        /// <summary>
        /// Writes a warning log message and attaches caller information automatically.
        /// </summary>
        /// <param name="logger">The logger that writes the message.</param>
        /// <param name="message">The human-readable message to log.</param>
        /// <param name="caller">The member name provided by the compiler.</param>
        /// <param name="file">The source file path provided by the compiler.</param>
        /// <param name="lineNumber">The source line number provided by the compiler.</param>
        public static void LogWarningWithCallerInfo(this ILogger logger, string message,
            [CallerMemberName] string caller = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            logger.LogWarning("{File}:{Line} [{Caller}] - {Message}", file, lineNumber, caller, message);
        }

        /// <summary>
        /// Writes an error log message and attaches caller information automatically.
        /// </summary>
        /// <param name="logger">The logger that writes the message.</param>
        /// <param name="message">The human-readable message to log.</param>
        /// <param name="caller">The member name provided by the compiler.</param>
        /// <param name="file">The source file path provided by the compiler.</param>
        /// <param name="lineNumber">The source line number provided by the compiler.</param>
        public static void LogErrorWithCallerInfo(this ILogger logger, string message,
                                                    [CallerMemberName] string caller = "",
                                                    [CallerFilePath] string file = "",
                                                    [CallerLineNumber] int lineNumber = 0)
        {
            logger.LogError("{File}:{Line} [{Caller}] - {Message}", file, lineNumber, caller, message);
        }

    }
}
