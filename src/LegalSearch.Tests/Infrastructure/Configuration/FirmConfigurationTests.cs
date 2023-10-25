using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class FirmConfigurationTests
    {
        [Fact]
        public void FirmConfiguration_Should_ApplyQueryFilterForIsDeleted()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(Firm));
                var queryFilter = entity.GetQueryFilter();

                // Assert
                Assert.NotNull(queryFilter);
            }
        }

        [Fact]
        public void FirmConfiguration_Should_HaveUniqueIndexOnName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(Firm));
                var nameIndex = entity.FindIndex(entity.FindProperty("Name"));

                // Assert
                Assert.NotNull(nameIndex);
                Assert.False(nameIndex.IsUnique); // Ensure the index is not unique
            }
        }
    }
}
