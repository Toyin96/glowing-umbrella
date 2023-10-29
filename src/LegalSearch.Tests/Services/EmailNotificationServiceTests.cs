using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Infrastructure.Services.Notification;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace LegalSearch.Test.Services
{
    public class EmailNotificationServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_Should_ReturnTrue_When_EmailIsSentSuccessfully()
        {
            // Arrange
            var sendEmailRequest = new SendEmailRequest
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "sample mail",
                Body = "This is a sample mail"
            };

            var httpClientFactory = GetMockHttpClientFactory(HttpStatusCode.OK);
            var logger = new Mock<ILogger<EmailNotificationService>>();

            var emailService = new EmailNotificationService(httpClientFactory, logger.Object);

            // Act
            var result = await emailService.SendEmailAsync(sendEmailRequest);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SendEmailAsync_Should_ReturnFalse_When_EmailFailsToSend()
        {
            // Arrange
            var sendEmailRequest = new SendEmailRequest
            {
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "sample mail",
                Body = "This is a sample mail"
            };

            var httpClientFactory = GetMockHttpClientFactory(HttpStatusCode.InternalServerError);
            var logger = new Mock<ILogger<EmailNotificationService>>();

            var emailService = new EmailNotificationService(httpClientFactory, logger.Object);

            // Act
            var result = await emailService.SendEmailAsync(sendEmailRequest);

            // Assert
            Assert.False(result);
        }

        private IHttpClientFactory GetMockHttpClientFactory(HttpStatusCode statusCode)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent("Response content here")
                });

            var httpClient = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://example.com/")
            };

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return httpClientFactory.Object;
        }
    }
}
