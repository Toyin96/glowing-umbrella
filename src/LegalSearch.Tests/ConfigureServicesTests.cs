using Fcmb.Shared.Models.Constants;
using LegalSearch.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LegalSearch.Test
{
    public class ConfigureServicesTests
    {
        [Fact]
        public void ConfigureInfrastructureServices_ConfiguresServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(); // Replace with your appsettings configuration

            // Act
            ConfigureServices.ConfigureInfrastructureServices(services, configuration);

            // Assert
            Assert.NotNull(services.BuildServiceProvider().GetService<IMediator>());
            Assert.NotNull(services.BuildServiceProvider().GetService<IHttpClientFactory>());
        }

        [Fact]
        public void ConfigureIdentity_SetsUpIdentityOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            ConfigureServices.ConfigureIdentity(services);
            var serviceProvider = services.BuildServiceProvider();
            var identityOptions = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;

            // Assert
            Assert.True(identityOptions.User.RequireUniqueEmail);
            Assert.Equal(8, identityOptions.Password.RequiredLength);
        }

        [Fact]
        public void ConfigureAuthHttpClient_ConfiguresAuthHttpClient()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(); // Replace with your appsettings configuration

            // Act
            ConfigureServices.ConfigureAuthHttpClient(services, configuration);

            // Assert
            var authHttpClient = services.BuildServiceProvider().GetService<IHttpClientFactory>().CreateClient(HttpConstants.AuthHttpClient);
            Assert.NotNull(authHttpClient.BaseAddress);
        }
    }
}
