using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class LegalRequestConfigurationTests
    {
        [Fact]
        public void LegalRequestConfiguration_Should_HaveSupportingDocumentsNavigationProperty()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(LegalRequest));
                var navigation = entity.FindNavigation("SupportingDocuments");

                // Assert
                Assert.NotNull(navigation);
                Assert.Equal(nameof(LegalRequest.SupportingDocuments), navigation.Name);

                // Ensure the relationship is defined as required and not owned
                var relationship = navigation.ForeignKey;
                Assert.False(relationship.IsRequired);
                Assert.False(relationship.IsOwnership);
            }
        }
    }
}
