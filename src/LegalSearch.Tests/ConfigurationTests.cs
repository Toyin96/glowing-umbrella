using Hangfire;
using LegalSearch.Api;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Logging;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LegalSearch.Test
{
    public class ConfigurationTests
    {
        [Fact]
        public void ConfigureAuthentication_ConfiguresJWTAuthentication_Added()
        {
            // Arrange
            var services = new ServiceCollection();

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to your project directory
                .AddJsonFile("appsettings.json") // Load the appsettings.json file
                .Build();

            // Act
            services.ConfigureAuthentication(configuration);

            // Assert
            var provider = services.BuildServiceProvider();
            var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

            // Check if JWT authentication scheme is added
            Assert.Contains(authenticationOptions.Schemes, scheme => scheme.Name == JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public async Task CreateAdminUser_AdminUserDoesNotExist_CreatesUser()
        {
            // Arrange
            var configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configuration.Setup(c => c[AppConstants.AdminEmail]).Returns("admin@example.com");
            configuration.Setup(c => c[AppConstants.AdminPassword]).Returns("adminPassword");
            configuration.Setup(c => c[AppConstants.AdminFirstName]).Returns("AdminFirstName");

            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);

            userManager.Setup(um => um.FindByNameAsync("admin@example.com")).ReturnsAsync((User)null);

            // Act
            await ConfigureServices.CreateAdminUser(configuration.Object, userManager.Object);

            // Assert
            userManager.Verify(um => um.CreateAsync(It.IsAny<User>(), "adminPassword"), Times.Once);
            userManager.Verify(um => um.AddToRoleAsync(It.IsAny<User>(), RoleType.Admin.ToString()), Times.Once);
        }

        [Fact]
        public void AddServicesToContainer_AddsHttpClient()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            services.AddServicesToContainer(configuration);
            var serviceProvider = services.BuildServiceProvider();

            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient("notificationClient");

            Assert.NotNull(client);
        }

        [Fact]
        public void ConfigureLoggingCapability_ConfiguresLogger()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build(); 
            
            var loggerOptions = configuration.GetSection("Logging").Get<LoggerOptions>();

            // Act
            services.ConfigureLoggingCapability(loggerOptions);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("TestLogger");

            // Validate the logger configuration
            Assert.NotNull(logger);
        }

        [Fact]
        public void ConfigureDatabase_ConfiguresDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            // Act
            services.ConfigureDatabase(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            var dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();

            Assert.NotNull(dbContext);
            Assert.NotNull(dbContextOptions);
        }

        [Fact]
        public void ConfigureHangFire_ConfiguresHangfire()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(); 

            // Act
            services.ConfigureHangFire(configuration);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var hangfireJobStorage = serviceProvider.GetRequiredService<JobStorage>();
            var hangfireOptions = serviceProvider.GetRequiredService<IGlobalConfiguration>();

            Assert.NotNull(hangfireJobStorage);
            Assert.NotNull(hangfireOptions);
        }

        [Fact]
        public void RegisterHttpRequiredServices_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configuration.Setup(c => c["FCMBConfig:EmailConfig:EmailUrl"]).Returns("http://example.com");
            configuration.Setup(c => c["FCMBConfig:ClientId"]).Returns("clientId");

            // Act
            services.RegisterHttpRequiredServices(configuration.Object);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            Assert.NotNull(httpClientFactory);
        }
    }
}
