using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LegalSearch.Api.HealthCheck
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _dbContext;

        public DatabaseHealthCheck(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Attempt a database query
                var result = await _dbContext.Branches.FirstOrDefaultAsync();

                if (result != null)
                    return HealthCheckResult.Healthy("Database is healthy.");
                else
                    return HealthCheckResult.Unhealthy("Database is unhealthy.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed: " + ex.Message);
            }
        }
    }

}
