﻿using LegalSearch.Application.Models.Requests;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LegalSearch.Api.HealthCheck
{
    /// <summary>
    /// Health check for an API using HttpClient.
    /// </summary>
    public class ApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly FCMBConfig _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiHealthCheck"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        public ApiHealthCheck(HttpClient httpClient, IOptions<FCMBConfig> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        /// <summary>
        /// Performs the API health check by sending a request to the API's health endpoint.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="HealthCheckResult"/> indicating the API's health status.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Replace with an actual test endpoint in your API
                _httpClient.BaseAddress = new Uri(_options.ApplicationBaseUrl);
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
