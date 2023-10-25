using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class NumericTokenProviderTests
    {
        [Fact]
        public async void CanGenerateTwoFactorTokenAsync_ShouldReturnTrue()
        {
            // Arrange
            var manager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            var user = new User() { FirstName = "Test"};
            var tokenProvider = new NumericTokenProvider<User>();

            // Act
            var result = await tokenProvider.CanGenerateTwoFactorTokenAsync(manager.Object, user);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async void GenerateAsync_ShouldGenerateValidNumericToken()
        {
            // Arrange
            var manager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            var user = new User() { FirstName = "Test" };
            var tokenProvider = new NumericTokenProvider<User>();

            // Act
            var token = await tokenProvider.GenerateAsync("test-purpose", manager.Object, user);

            // Assert
            Assert.True(int.TryParse(token, out int numericToken));
            Assert.True(numericToken >= 1000 && numericToken <= 9999);
        }

        [Fact]
        public async void ValidateAsync_ShouldReturnTrueForValidNumericToken()
        {
            // Arrange
            var manager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            var user = new User() { FirstName = "Test" };
            var tokenProvider = new NumericTokenProvider<User>();
            var validNumericToken = "1234";

            // Act
            var result = await tokenProvider.ValidateAsync("test-purpose", validNumericToken, manager.Object, user);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async void ValidateAsync_ShouldReturnFalseForInvalidNumericToken()
        {
            // Arrange
            var manager = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
            var user = new User() { FirstName = "Test" };
            var tokenProvider = new NumericTokenProvider<User>();
            var invalidNumericToken = "invalid";

            // Act
            var result = await tokenProvider.ValidateAsync("test-purpose", invalidNumericToken, manager.Object, user);

            // Assert
            Assert.False(result);
        }
    }
}
