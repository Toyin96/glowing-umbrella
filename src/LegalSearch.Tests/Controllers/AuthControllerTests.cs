using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace LegalSearch.Tests.Controllers
{
    public class AuthControllerTests
    {
        private AuthController _authController;
        private Mock<IGeneralAuthService<User>> _mockAuthService;

        public AuthControllerTests()
        {
            // Arrange
            _mockAuthService = new Mock<IGeneralAuthService<User>>();
            _authController = new AuthController(_mockAuthService.Object);
        }

        [Fact]
        public async Task UserLogin_ValidRequest_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "password123"
            };
            var loginResponse = new LoginResponse
            {
                Token = "eyryhr7r75yr7545tuituiui568"
            };

            _mockAuthService.Setup(x => x.UserLogin(loginRequest)).ReturnsAsync(new ObjectResponse<LoginResponse>("Success", ResponseCodes.Success)
            {
                Data = loginResponse
            });

            // Act
            var result = await _authController.UserLogin(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<LoginResponse>>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("eyryhr7r75yr7545tuituiui568", response.Data.Token);
        }

        [Fact]
        public async Task RequestUnlockCode_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new RequestUnlockCodeRequest { Email = "sample@example.com" };
            var mockResponse = new StatusResponse("Unlock code sent successfully", ResponseCodes.Success);
            _mockAuthService.Setup(x => x.RequestUnlockCode(request)).ReturnsAsync(mockResponse);

            // Act
            var result = await _authController.RequestUnlockCode(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Unlock code sent successfully", response.Description);
        }

        [Fact]
        public async Task ReIssueToken_AuthenticatedUser_ReturnsOk()
        {
            // Arrange
            var userId = "123"; // Sample user ID
            var mockResponse = new ReIssueTokenResponse { Token = "newToken" };

            // Mock authenticated user
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(nameof(ClaimType.UserId), userId)
            }, "mock");

            var user = new ClaimsPrincipal(identity);

            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mockAuthService.Setup(x => x.ReIssueToken(userId)).ReturnsAsync(new ObjectResponse<ReIssueTokenResponse>("Success", ResponseCodes.Success) { Data = mockResponse });

            // Act
            var result = await _authController.ReIssueToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<ReIssueTokenResponse>>(okResult.Value);
            Assert.Equal(mockResponse.Token, response.Data.Token);
        }

        [Fact]
        public async Task UnlockAccount_ValidRequest_ReturnsOk()
        {
            // Arrange
            var unlockAccountRequest = new UnlockAccountRequest { Email = "sample_use1@example.com", UnlockCode = "123456" };

            // Mock the authentication service
            var mockAuthService = new Mock<IGeneralAuthService<User>>();
            mockAuthService.Setup(service => service.UnlockCode(It.IsAny<UnlockAccountRequest>()))
                           .ReturnsAsync(new StatusResponse("Success", ResponseCodes.Success));

            var authController = new AuthController(mockAuthService.Object);

            // Act
            var result = await authController.UnlockAccount(unlockAccountRequest);

            // Assert

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
        }

        [Fact]
        public async Task VerifyTwoFactor_ValidRequest_ReturnsOk()
        {
            // Arrange
            var twoFactorVerificationRequest = new TwoFactorVerificationRequest { Email = "sample_use1@example.com", TwoFactorCode = "123456" };

            // Mock the authentication service
            var mockAuthService = new Mock<IGeneralAuthService<User>>();
            mockAuthService.Setup(service => service.Verify2fa(It.IsAny<TwoFactorVerificationRequest>()))
                           .ReturnsAsync(new ObjectResponse<LoginResponse>("Operation was successful", ResponseCodes.Success)
                           {
                               Data = new LoginResponse
                               {
                                   Token = "3dhfyfrytygujhihj",
                                   DisplayName = "Test",
                                   Role = "Solicitor"
                               }
                           });

            var authController = new AuthController(mockAuthService.Object);

            // Act
            var result = await authController.VerifyTwoFactor(twoFactorVerificationRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<LoginResponse>>(okResult.Value);
            Assert.Equal("3dhfyfrytygujhihj", response.Data.Token);
            Assert.Equal("Test", response.Data.DisplayName);
            Assert.Equal("Solicitor", response.Data.Role);
        }
    }
}
