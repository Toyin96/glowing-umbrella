using LegalSearch.Domain.Entities.Location;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class ZonalServiceManagerConfigurationTests
    {
        [Fact]
        public void ZonalServiceManagerConfiguration_Should_HaveManyBranchesRelationship()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(ZonalServiceManager));
                var relationship = entity.FindNavigation(nameof(ZonalServiceManager.Branches)).ForeignKey;

                // Assert
                Assert.NotNull(relationship);
                Assert.False(relationship.IsOwnership);
                Assert.Equal(nameof(Branch.ZonalServiceManagerId), relationship.Properties.First().Name);
                Assert.Equal("Id", relationship.PrincipalKey.Properties.First().Name);
                Assert.Equal(DeleteBehavior.Restrict, relationship.DeleteBehavior);
            }
        }
    }
}
