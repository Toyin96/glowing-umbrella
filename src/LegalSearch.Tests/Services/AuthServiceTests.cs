using Fcmb.Shared.Auth.Models.Requests;
using LegalSearch.Infrastructure.Services.User;
using LegalSearch.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace LegalSearch.Tests.Services
{
    public class AuthServiceTests
    {

        private readonly AuthService _authService;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public AuthServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockConfiguration = new Mock<IConfiguration>();

            _authService = new AuthService(_mockHttpClientFactory.Object, _mockLogger.Object, _mockConfiguration.Object);
        }

        //[Fact]
        //public async Task LoginAsync_WithValidRequest_ShouldReturnSuccessfulResponse()
        //{
        //    // Arrange
        //    var loginRequest = new LoginRequest
        //    {
        //        Email = "test@example.com",
        //        Password = "password123"
        //    };

        //    var httpClient = new Mock<HttpClient>();
        //    var httpResponseMessage = new HttpResponseMessage
        //    {
        //        StatusCode = HttpStatusCode.OK,
        //        Content = new StringContent(GetSuccessfulLoginXmlResponse(), Encoding.UTF8, "text/xml")
        //    };

        //    httpClient.SetupPostAsync(httpResponseMessage);
        //    _mockHttpClientFactory.Setup(factory => factory.CreateClient()).Returns(httpClient.Object);

        //    // Act
        //    var response = await _authService.LoginAsync(loginRequest);

        //    // Assert
        //    Assert.Equal("00", response.Code);
        //    Assert.NotNull(response.Data);
        //}

        //[Fact]
        //public async Task LoginAsync_WithInvalidRequest_ShouldReturnErrorResponse()
        //{
        //    // Arrange
        //    var loginRequest = new LoginRequest
        //    {
        //        Email = "test@example.com",
        //        Password = "invalidpassword"
        //    };

        //    var responseXml = GetErrorXmlResponse();
        //    var content = new StringContent(responseXml, Encoding.UTF8, "text/xml");

        //    var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        //    mockHttpMessageHandler.Protected()
        //        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        //        .ReturnsAsync(new HttpResponseMessage
        //        {
        //            StatusCode = HttpStatusCode.OK,
        //            Content = content
        //        });

        //    var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        //    _mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
        //        .Returns(httpClient);

        //    // Act
        //    var response = await _authService.LoginAsync(loginRequest);

        //    // Assert
        //    Assert.Equal("999", response.Code);
        //    Assert.NotNull(response.Data);
        //}

        // Helper method to generate a successful login XML response
        private string GetSuccessfulLoginXmlResponse()
        {
            // Construct the XML response for a successful login.
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
                  <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"">
                    <soapenv:Body>
                      <tem:Response>00</tem:Response>
                      <tem:StaffID>12345</tem:StaffID>
                      <tem:StaffName>John Doe</tem:StaffName>
                      <tem:DisplayName>John</tem:DisplayName>
                      <tem:Department>IT</tem:Department>
                      <tem:Groups>Group1,Group2</tem:Groups>
                      <tem:ManagerName>Jane Doe</tem:ManagerName>
                      <tem:ManagerDepartment>IT Management</tem:ManagerDepartment>
                    </soapenv:Body>
                  </soapenv:Envelope>";
        }

        // Helper method to generate an error XML response
        private string GetErrorXmlResponse()
        {
            // Construct the XML response for an error.
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
                  <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"">
                    <soapenv:Body>
                      <tem:Response>999</tem:Response>
                    </soapenv:Body>
                  </soapenv:Envelope>";
        }
    }
}
