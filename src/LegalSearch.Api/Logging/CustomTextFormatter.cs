using Serilog.Events;
using Serilog.Formatting;

namespace LegalSearch.Api.Logging
{
    /// <summary>
    /// A custom text formatter for distinguishing the different log levels
    /// </summary>
    /// <seealso cref="Serilog.Formatting.ITextFormatter" />
    public class CustomTextFormatter : ITextFormatter
    {
        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            // Include log level
            output.Write($"[{logEvent.Level}] ");

            // Include timestamp
            output.Write($"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss} ");

            // Include message
            output.Write($"{logEvent.RenderMessage()}");

            // Add new line for next log
            output.WriteLine();
        }
    }

}
