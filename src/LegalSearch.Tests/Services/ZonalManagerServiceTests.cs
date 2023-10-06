using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Infrastructure.Services.User;
using Moq;

namespace LegalSearch.Tests.Services
{
    public class ZonalManagerServiceTests
    {
        private readonly Mock<IZonalServiceManager> _mockZonalServiceManager;
        private readonly ZonalManagerService _zonalManagerService;

        public ZonalManagerServiceTests()
        {
            _mockZonalServiceManager = new Mock<IZonalServiceManager>();
            _zonalManagerService = new ZonalManagerService(_mockZonalServiceManager.Object);
        }

        [Fact]
        public async Task GetZonalServiceManagers_ShouldReturnZonalServiceManagers()
        {
            // Arrange
            var zonalServiceManagers = new List<ZonalServiceManagerMiniDto>
        {
            new ZonalServiceManagerMiniDto { Id = Guid.NewGuid(), Name = "Manager 1", EmailAddress = "test_zsm1@fcmb.com" },
            new ZonalServiceManagerMiniDto { Id = Guid.NewGuid(), Name = "Manager 2", EmailAddress = "test_zsm2@fcmb.com" }
        };

            _mockZonalServiceManager.Setup(manager => manager.GetAllZonalServiceManagersInfo())
                .ReturnsAsync(zonalServiceManagers);

            // Act
            var response = await _zonalManagerService.GetZonalServiceManagers();

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(zonalServiceManagers.Count, response.Total);
            Assert.Equal(zonalServiceManagers, response.Data);
        }
    }

}
