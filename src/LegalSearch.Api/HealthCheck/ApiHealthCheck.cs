using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LegalSearch.Api.HealthCheck
{
    public class ApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;

        public ApiHealthCheck(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Replace with an actual test endpoint in your API
                var response = await _httpClient.GetAsync("api/health");

                if (response.IsSuccessStatusCode)
                    return HealthCheckResult.Healthy("API is healthy.");
                else
                    return HealthCheckResult.Unhealthy("API is unhealthy: " + response.ReasonPhrase);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("API check failed: " + ex.Message);
            }
        }
    }

}
