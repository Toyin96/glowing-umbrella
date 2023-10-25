using Microsoft.EntityFrameworkCore;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Domain.Entities.AuditLog;

namespace LegalSearch.Test.Infrastructure.Configuration
{
    public class AuditLogConfigurationTests
    {
        [Fact]
        public void AuditLogConfiguration_Should_ApplyQueryFilterForIsDeleted()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Act
                var entity = dbContext.Model.FindEntityType(typeof(AuditLog));
                var queryFilter = entity.GetQueryFilter();

                // Assert
                Assert.NotNull(queryFilter);
            }
        }
    }
}
