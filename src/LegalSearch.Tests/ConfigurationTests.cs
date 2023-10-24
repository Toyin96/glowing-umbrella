using LegalSearch.Api;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            var configuration = new Mock<IConfiguration>();
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
    }
}
