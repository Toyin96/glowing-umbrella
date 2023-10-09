namespace LegalSearch.Application.Models.Logging
{
    public class LoggerOptions
    {
        public bool EnableConsole { get; set; }
        public bool EnableFile { get; set; }
        public string? LogFilePath { get; set; }
        public int RetainedFileCountLimit { get; set; }
        public long? FileSizeLimitBytes { get; set; }
        public bool RollOnFileSizeLimit { get; set; }
        public bool EnableAzureApplicationInsights { get; set; }
        public string? AzureInstrumentationKey { get; set; }
    }
}
