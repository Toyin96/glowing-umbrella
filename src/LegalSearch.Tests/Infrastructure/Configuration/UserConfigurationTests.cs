using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class UserConfigurationTests
    {
        [Fact]
        public void UserConfiguration_Should_HaveQueryFilterForIsDeleted()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase1")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(User));
                var queryFilter = entity.GetQueryFilter();

                // Assert
                Assert.NotNull(queryFilter);
            }
        }

        [Fact]
        public void UserConfiguration_Should_HaveIndexOnId()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(User));
                var index = entity.FindIndex(entity.FindProperty("Id"));

                // Assert
                Assert.NotNull(index);
            }
        }

        [Fact]
        public void UserConfiguration_Should_HaveOptionalLastName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase3")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(User));
                var lastNameProperty = entity.FindProperty("LastName");

                // Assert
                Assert.NotNull(lastNameProperty);
                Assert.True(lastNameProperty.IsNullable);
            }
        }

        [Fact]
        public void UserConfiguration_Should_HaveOptionalFirmId()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase4")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(User));
                var firmIdProperty = entity.FindProperty("FirmId");

                // Assert
                Assert.NotNull(firmIdProperty);
                Assert.True(firmIdProperty.IsNullable);
            }
        }

        [Fact]
        public void UserConfiguration_Should_HaveOptionalRelationshipWithFirm()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase6")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(User));
                var relationship = entity.FindNavigation(nameof(User.Firm)).ForeignKey;

                // Assert
                Assert.NotNull(relationship);
                Assert.Equal(nameof(User.FirmId), relationship.Properties.First().Name);
                Assert.Equal("Id", relationship.PrincipalKey.Properties.First().Name);
                Assert.Equal(DeleteBehavior.Restrict, relationship.DeleteBehavior);
            }
        }
    }
}
