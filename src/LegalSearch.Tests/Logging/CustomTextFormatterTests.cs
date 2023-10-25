using LegalSearch.Api.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace LegalSearch.Test.Logging
{
    public class CustomTextFormatterTests
    {
        [Fact]
        public void Format_LogEventFormatsCorrectly()
        {
            // Arrange
            var formatter = new CustomTextFormatter();
            var logEvent = new LogEvent(
                DateTimeOffset.Parse("2023-09-13 15:30:45"),
                LogEventLevel.Information,
                null,
                new MessageTemplate(new MessageTemplateToken[] { new TextToken("Log message here") }),
                new LogEventProperty[0]);

            var expectedLogEntry = "[Information] 2023-09-13 15:30:45 Log message here\r\n";
            var output = new StringWriter();

            // Act
            formatter.Format(logEvent, output);

            // Assert
            var actualLogEntry = output.ToString();
            Assert.Equal(expectedLogEntry, actualLogEntry);
        }
    }
}
