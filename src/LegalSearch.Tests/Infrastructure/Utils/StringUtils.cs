using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class StringUtilsTests
    {
        [Fact]
        public void First10Characters_WithEmptyString_ShouldReturnEmptyString()
        {
            // Arrange
            string input = string.Empty;

            // Act
            string result = input.First10Characters();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void First10Characters_WithShortString_ShouldReturnSameString()
        {
            // Arrange
            string input = "Hello";

            // Act
            string result = input.First10Characters();

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void First10Characters_WithLongString_ShouldReturnFirst10Characters()
        {
            // Arrange
            string input = "ThisIsALongString";

            // Act
            string result = input.First10Characters();

            // Assert
            Assert.Equal("ThisIsALon", result);
        }
    }
}
