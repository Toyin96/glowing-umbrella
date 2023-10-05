using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LegalSearch.Api.HealthCheck
{
    /// <summary>
    /// Health check for a database using AppDbContext.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHealthCheck"/> class.
        /// </summary>
        /// <param name="dbContext">The AppDbContext instance.</param>
        public DatabaseHealthCheck(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Performs the database health check by attempting a database query.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="HealthCheckResult"/> indicating the database's health status.</returns>
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
