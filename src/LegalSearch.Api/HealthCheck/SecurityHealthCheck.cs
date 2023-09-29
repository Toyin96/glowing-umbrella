using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LegalSearch.Api.HealthCheck
{
    public class SecurityHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Implement security checks here
                // Example: check if SSL/TLS certificate is valid

                // Replace with your actual security checks
                bool isSecurityHealthy = true;

                if (isSecurityHealthy)
                    return Task.FromResult(HealthCheckResult.Healthy("Security is healthy."));
                else
                    return Task.FromResult(HealthCheckResult.Unhealthy("Security is not healthy."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Security check failed: " + ex.Message));
            }
        }
    }

}
