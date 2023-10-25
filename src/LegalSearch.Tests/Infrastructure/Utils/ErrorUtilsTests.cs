using Microsoft.AspNetCore.Identity;
using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class ErrorUtilsTests
    {
        [Fact]
        public void GetStandardizedError_WithErrors_ReturnsConcatenatedErrors()
        {
            // Arrange
            var identityResult = IdentityResult.Failed(new IdentityError
            {
                Code = "Error1",
                Description = "Error description 1"
            },
            new IdentityError
            {
                Code = "Error2",
                Description = "Error description 2"
            });

            // Act
            var result = identityResult.GetStandardizedError();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Error description 1", result);
            Assert.Contains("Error description 2", result);
        }

        [Fact]
        public void GetStandardizedError_WithNoErrors_ReturnsNull()
        {
            // Arrange
            var identityResult = IdentityResult.Success;

            // Act
            var result = identityResult.GetStandardizedError();

            // Assert
            Assert.Null(result);
        }
    }
}
