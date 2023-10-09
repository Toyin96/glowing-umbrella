using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly RoleService _roleService;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly Mock<AppDbContext> _mockDbContext;

        public RoleServiceTests()
        {
            _mockRoleManager = new Mock<RoleManager<Role>>(
                new Mock<IQueryableRoleStore<Role>>().Object,
                null, null, null, null);

            _mockLogger = new Mock<ILogger<RoleService>>();
            _mockDbContext = new Mock<AppDbContext>();

            _roleService = new RoleService(
                _mockRoleManager.Object,
                _mockLogger.Object,
                _mockDbContext.Object);
        }

        // Tests for CreateRoleAsync method
        [Fact]
        public async Task CreateRoleAsync_ValidRoleRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var roleRequest = new RoleRequest { RoleName = "TestRole" };
            _mockRoleManager.Setup(rm => rm.RoleExistsAsync(roleRequest.RoleName)).ReturnsAsync(false);
            _mockRoleManager.Setup(rm => rm.CreateAsync(It.IsAny<Role>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var response = await _roleService.CreateRoleAsync(roleRequest);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task CreateRoleAsync_RoleNameNullOrEmpty_ThrowsArgumentNullException()
        {
            // Arrange
            var roleRequest = new RoleRequest { RoleName = null };

            // Act and Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _roleService.CreateRoleAsync(roleRequest));
        }

        // Tests for GetRoleByIdAsync method
        [Fact]
        public async Task GetRoleByIdAsync_WithValidId_ReturnsObjectResponse()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "TestRole" };
            _mockDbContext.Setup(db => db.Roles.FindAsync(roleId)).ReturnsAsync(role);

            // Act
            var response = await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(roleId, response.Data.RoleId);
        }

        // Tests for GetRoleByNameAsync method
        [Fact]
        public async Task GetRoleByNameAsync_WithValidName_ReturnsObjectResponse()
        {
            // Arrange
            var roleName = "TestRole";
            var role = new Role { Id = Guid.NewGuid(), Name = roleName };
            _mockRoleManager.Setup(rm => rm.FindByNameAsync(roleName)).ReturnsAsync(role);

            // Act
            var response = await _roleService.GetRoleByNameAsync(roleName);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(roleName, response.Data.RoleName);
        }


        // Tests for FilterRoleQuery method
        [Fact]
        public void FilterRoleQuery_WithRoleNameFilter_ReturnsFilteredQuery()
        {
            // Arrange
            var roles = new List<RoleResponse>
            {
                new RoleResponse { RoleName = "Role1" },
                new RoleResponse { RoleName = "Role2" },
                new RoleResponse { RoleName = "OtherRole" }
            }.AsQueryable();

            // Act
            var filteredQuery = _roleService.FilterRoleQuery(roles, new FilterRoleRequest { RoleName = "Role" });

            // Assert
            Assert.Equal(3, filteredQuery.Count());
        }

    }

}
