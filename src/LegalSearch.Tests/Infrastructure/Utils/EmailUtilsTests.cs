using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class EmailUtilsTests
    {
        [Fact]
        public async Task UpdatePlaceHolders_Should_ReturnOriginalText_When_TextIsNull()
        {
            // Arrange
            string text = null;
            var keyValuePairs = new List<KeyValuePair<string, string>>();

            // Act
            var result = await EmailUtils.UpdatePlaceHolders(text, keyValuePairs);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePlaceHolders_Should_ReturnOriginalText_When_TextIsEmpty()
        {
            // Arrange
            string text = "";
            var keyValuePairs = new List<KeyValuePair<string, string>>();

            // Act
            var result = await EmailUtils.UpdatePlaceHolders(text, keyValuePairs);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public async Task UpdatePlaceHolders_Should_ReturnOriginalText_When_KeyValuePairsAreNull()
        {
            // Arrange
            string text = "Hello, {Name}!";
            List<KeyValuePair<string, string>> keyValuePairs = null;

            // Act
            var result = await EmailUtils.UpdatePlaceHolders(text, keyValuePairs);

            // Assert
            Assert.Equal("Hello, {Name}!", result);
        }

        [Fact]
        public async Task UpdatePlaceHolders_Should_ReplacePlaceholders_When_TextContainsPlaceholders()
        {
            // Arrange
            string text = "Hello, {Name}! Your email is {Email}.";
            var keyValuePairs = new List<KeyValuePair<string, string>>
            {
            new KeyValuePair<string, string>("{Name}", "John"),
            new KeyValuePair<string, string>("{Email}", "john@example.com")
            };

            // Act
            var result = await EmailUtils.UpdatePlaceHolders(text, keyValuePairs);

            // Assert
            Assert.Equal("Hello, John! Your email is john@example.com.", result);
        }
    }
}
