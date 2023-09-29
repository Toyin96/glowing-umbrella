using Serilog.Events;
using Serilog;
using LegalSearch.Application.Models.Logging;

namespace LegalSearch.Api.Logging
{
    /// <summary>
    /// Service for configuring the logger.
    /// </summary>
    public interface ILoggerConfigurationService
    {
        /// <summary>
        /// Configures the logger based on the provided options.
        /// </summary>
        /// <param name="options">The logger configuration options.</param>
        LoggerConfiguration ConfigureLogger(LoggerOptions options);
    }

    /// <summary>
    /// Service implementation for configuring the logger.
    /// </summary>
    /// <seealso cref="LegalSearch.Api.Logging.ILoggerConfigurationService" />
    public class LoggerConfigurationService : ILoggerConfigurationService
    {
        private readonly IConfiguration _configuration;

        // Constructor to inject IConfiguration
        public LoggerConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Implementing the ConfigureLogger method
        public LoggerConfiguration ConfigureLogger(LoggerOptions options)
        {
            // Initialize Serilog logger configuration
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext();

            // Configure console logging if enabled
            if (options.EnableConsole)
                loggerConfiguration.WriteTo.Console();

            // Configure file logging if enabled and a valid file path is provided
            if (options.EnableFile && !string.IsNullOrEmpty(options.LogFilePath))
                loggerConfiguration.WriteTo.File(
                    formatter: new CustomTextFormatter(),
                    path: options.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    fileSizeLimitBytes: options.FileSizeLimitBytes,
                    rollOnFileSizeLimit: options.RollOnFileSizeLimit);

            // Configure Azure Application Insights logging if enabled and a valid instrumentation key is provided
            if (options.EnableAzureApplicationInsights && !string.IsNullOrEmpty(options.AzureInstrumentationKey))
                loggerConfiguration.WriteTo.ApplicationInsights(options.AzureInstrumentationKey, TelemetryConverter.Traces);

            // Return the configured logger configuration
            return loggerConfiguration;
        }
    }
}
