using LegalSearch.Domain.Entities.Role;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class RoleConfigurationTests
    {
        [Fact]
        public void RoleConfiguration_Should_HaveQueryFilter()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(Role));

                // Assert
                Assert.True(entity.GetQueryFilter() != null, "Role should have a query filter.");
            }
        }

        [Fact]
        public void RoleConfiguration_Should_HaveUniqueIndexOnName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(Role));
                var index = entity.FindIndex(entity.FindProperty("Name"));

                // Assert
                Assert.NotNull(index);
                Assert.True(index.IsUnique, "The index on 'Name' should be unique.");
            }
        }
    }
}
