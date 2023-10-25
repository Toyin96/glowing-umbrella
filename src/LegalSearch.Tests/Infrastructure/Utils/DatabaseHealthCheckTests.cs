using LegalSearch.Api.HealthCheck;
using LegalSearch.Domain.Entities.Location;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class DatabaseHealthCheckTests
    {
        [Fact]
        public async Task CheckHealthAsync_DatabaseIsHealthy_ReturnsHealthyResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new AppDbContext(options);

            dbContext.Branches.Add(new Branch { Address = "sample address", SolId = "061" });
            dbContext.SaveChanges();

            var healthCheck = new DatabaseHealthCheck(dbContext);
            var healthCheckContext = new HealthCheckContext();

            // Act
            var result = await healthCheck.CheckHealthAsync(healthCheckContext, CancellationToken.None);

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal("Database is healthy.", result.Description);
        }


        [Fact]
        public async Task CheckHealthAsync_DatabaseIsUnhealthy_ReturnsUnhealthyResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                // Simulate an unhealthy database state (e.g., by not adding any data)
                // In this case, you might not need to call dbContext.SaveChanges().

                var healthCheck = new DatabaseHealthCheck(dbContext);
                var healthCheckContext = new HealthCheckContext();

                // Act
                var result = await healthCheck.CheckHealthAsync(healthCheckContext, CancellationToken.None);

                // Assert
                Assert.Equal(HealthStatus.Unhealthy, result.Status);
                Assert.Equal("Database is unhealthy.", result.Description);
            }
        }
    }
}
