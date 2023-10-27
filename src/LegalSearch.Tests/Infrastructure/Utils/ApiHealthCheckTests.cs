using LegalSearch.Api.HealthCheck;
using LegalSearch.Application.Models.Requests;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class ApiHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_HealthyResponse_ReturnsHealthyResult()
        {
            // Arrange
            var config = new FCMBConfig
            {
                BaseUrl = "sample base url",
                ApplicationBaseUrl = "https://example.com",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(config);
            var content = "API is healthy."; // Simulated response content
            var httpClient = new HttpClient(new TestHttpMessageHandler(HttpStatusCode.OK, false));
            var apiHealthCheck = new ApiHealthCheck(httpClient, options);

            // Act
            var healthCheckResult = await apiHealthCheck.CheckHealthAsync(null);

            // Assert
            Assert.Equal(HealthStatus.Healthy, healthCheckResult.Status);
            Assert.Equal(content, healthCheckResult.Description);
        }


        [Fact]
        public async Task CheckHealthAsync_UnhealthyResponse_ReturnsUnhealthyResult()
        {
            // Arrange
            var config = new FCMBConfig
            {
                BaseUrl = "sample base url",
                ApplicationBaseUrl = "https://example.com",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(config);
            var httpClient = new HttpClient(new TestHttpMessageHandler(HttpStatusCode.InternalServerError));
            var apiHealthCheck = new ApiHealthCheck(httpClient, options);

            // Act
            var healthCheckResult = await apiHealthCheck.CheckHealthAsync(null);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, healthCheckResult.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_ExceptionThrown_ReturnsUnhealthyResult()
        {
            // Arrange
            var config = new FCMBConfig
            {
                BaseUrl = "sample base url",
                ApplicationBaseUrl = "https://example.com",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(config);
            var httpClient = new HttpClient(new TestHttpMessageHandler(HttpStatusCode.OK, throwException: true));
            var apiHealthCheck = new ApiHealthCheck(httpClient, options);

            // Act
            var healthCheckResult = await apiHealthCheck.CheckHealthAsync(null);

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, healthCheckResult.Status);
        }

        // Helper class to simulate HttpClient responses
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode statusCode;
            private readonly bool throwException;

            public TestHttpMessageHandler(HttpStatusCode statusCode, bool throwException = false)
            {
                this.statusCode = statusCode;
                this.throwException = throwException;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (throwException)
                {
                    throw new Exception("Simulated exception");
                }

                var response = new HttpResponseMessage(statusCode);
                return Task.FromResult(response);
            }
        }
    }
}
