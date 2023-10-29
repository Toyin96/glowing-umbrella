using LegalSearch.Infrastructure.Services.User;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace LegalSearch.Tests.Services
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _jwtTokenService;

        public JwtTokenServiceTests()
        {
            // Set up the configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)  // Load appsettings.json
                .Build();

            _jwtTokenService = new JwtTokenService(configuration);
        }

        [Fact]
        public void GenerateJwtToken_ValidClaims_ReturnsToken()
        {
            // Arrange
            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, "test_user")
        });

            // Act
            var token = _jwtTokenService.GenerateJwtToken(identity);

            // Assert
            Assert.NotNull(token);
        }

        [Fact]
        public void ValidateJwtToken_ValidToken_ReturnsClaimsPrincipal()
        {
            // Arrange
            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, "test_user")
        });

            var token = _jwtTokenService.GenerateJwtToken(identity);

            // Act
            var principal = _jwtTokenService.ValidateJwtToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal("test_user", principal.Identity?.Name);
        }

        [Fact]
        public void ValidateJwtToken_InvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid_token";

            // Act
            var principal = _jwtTokenService.ValidateJwtToken(invalidToken);

            // Assert
            Assert.Null(principal);
        }
    }

}
