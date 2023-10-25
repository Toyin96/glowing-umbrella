using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class StateConfigurationTests
    {
        [Fact]
        public void StateConfiguration_Should_SetPrimaryKey()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(State));
                var primaryKey = entity.FindPrimaryKey();

                // Assert
                Assert.NotNull(primaryKey);
                Assert.Single(primaryKey.Properties, p => p.Name == "Id");
            }
        }

        [Fact]
        public void StateConfiguration_Should_HaveUniqueIndexOnName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(State));
                var index = entity.FindIndex(entity.FindProperty("Name"));

                // Assert
                Assert.NotNull(index);
                Assert.True(index.IsUnique, "The index on 'Name' should be unique.");
            }
        }

        [Fact]
        public void StateConfiguration_Should_HaveForeignKeyRelationshipWithRegion()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(State));
                var foreignKey = entity.FindNavigation(nameof(State.Region)).ForeignKey;

                // Assert
                Assert.NotNull(foreignKey);
                Assert.Equal(nameof(State.RegionId), foreignKey.Properties.First().Name);
                Assert.Equal("Id", foreignKey.PrincipalKey.Properties.First().Name);
                Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
            }
        }
    }
}
