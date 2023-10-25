using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Infrastructure.Services.User;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Test.Services
{
    public class CustomerManagerServiceTests
    {
        [Fact]
        public async Task GetCustomerServiceManagers_Success()
        {
            // Arrange
            var customerServiceManagerMock = new Mock<ICustomerServiceManager>();
            customerServiceManagerMock
                .Setup(m => m.GetCustomerServiceManagers())
                .ReturnsAsync(new List<CustomerServiceManagerMiniDto>
                {
                new CustomerServiceManagerMiniDto 
                { 
                    SolId = "061",
                    EmailAddress = "test@example.com",
                    Name = "Test",
                },
                    // Add more test data as needed
                });

            var loggerMock = new Mock<ILogger<CustomerManagerService>>();
            var service = new CustomerManagerService(customerServiceManagerMock.Object, loggerMock.Object);

            // Act
            var result = await service.GetCustomerServiceManagers();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Operation was success", result.Description);
            Assert.Equal(ResponseCodes.Success, result.Code);
        }
    }
}
