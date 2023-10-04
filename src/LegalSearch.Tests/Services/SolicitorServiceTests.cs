using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Tests.Services
{
    public class SolicitorServiceTests
    {
        private readonly SolicitorService _solicitorService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly Mock<ISolicitorManager> _mockSolicitorManager;
        private readonly Mock<ILogger<SolicitorService>> _mockLogger;
        private readonly Mock<UserManager<Domain.Entities.User.User>> _mockUserManager;
        private readonly Mock<ILegalSearchRequestManager> _mockLegalSearchRequestManager;

        public SolicitorServiceTests()
        {
            _mockDbContext = new Mock<AppDbContext>();
            _mockSolicitorManager = new Mock<ISolicitorManager>();
            _mockLogger = new Mock<ILogger<SolicitorService>>();
            _mockUserManager = MockUserManager.CreateMockUserManager();
            _mockLegalSearchRequestManager = new Mock<ILegalSearchRequestManager>();

            _solicitorService = new SolicitorService(
                _mockDbContext.Object, _mockSolicitorManager.Object,
                _mockLogger.Object, _mockUserManager.Object,
                _mockLegalSearchRequestManager.Object);
        }

        [Fact]
        public async Task ActivateOrDeactivateSolicitor_WithValidRequest_ShouldReturnSuccessfulResponse()
        {
            // Arrange
            var request = new ActivateOrDeactivateSolicitorRequest
            {
                SolicitorId = Guid.NewGuid(),
                ActionType = ProfileStatusActionType.Activate
            };

            var user = new Domain.Entities.User.User
            {
                Id = request.SolicitorId,
                ProfileStatus = ProfileStatusType.InActive.ToString()
            };

            _mockUserManager.Setup(mgr => mgr.FindByIdAsync(request.SolicitorId.ToString()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(mgr => mgr.UpdateAsync(It.IsAny<Domain.Entities.User.User>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var response = await _solicitorService.ActivateOrDeactivateSolicitor(request);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task ActivateOrDeactivateSolicitor_InvalidSolicitorId_ShouldReturnDataNotFoundResponse()
        {
            // Arrange
            var request = new ActivateOrDeactivateSolicitorRequest
            {
                SolicitorId = Guid.NewGuid(),
                ActionType = ProfileStatusActionType.Activate
            };

            _mockUserManager.Setup(mgr => mgr.FindByIdAsync(request.SolicitorId.ToString()))
                .ReturnsAsync((Domain.Entities.User.User)null);

            // Act
            var response = await _solicitorService.ActivateOrDeactivateSolicitor(request);

            // Assert
            Assert.Equal(ResponseCodes.DataNotFound, response.Code);
        }

        [Fact]
        public async Task ActivateOrDeactivateSolicitor_UpdateFailure_ShouldReturnConflictResponse()
        {
            // Arrange
            var request = new ActivateOrDeactivateSolicitorRequest
            {
                SolicitorId = Guid.NewGuid(),
                ActionType = ProfileStatusActionType.Activate
            };

            var user = new Domain.Entities.User.User
            {
                Id = request.SolicitorId,
                ProfileStatus = ProfileStatusType.InActive.ToString() // Set to inactive to test activation
            };

            _mockUserManager.Setup(mgr => mgr.FindByIdAsync(request.SolicitorId.ToString()))
                .ReturnsAsync(user);

            _mockUserManager.Setup(mgr => mgr.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "SomeErrorCode" }));

            // Act
            var response = await _solicitorService.ActivateOrDeactivateSolicitor(request);

            // Assert
            Assert.Equal(ResponseCodes.Conflict, response.Code);
        }

        [Fact]
        public async Task EditSolicitorProfile_ValidRequest_ShouldReturnSuccessResponse()
        {
            // Arrange
            var request = new EditSolicitorProfileByLegalTeamRequest
            {
                SolicitorId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FirmName = "Doe and Associates",
                Email = "john.doe@example.com",
                PhoneNumber = "1234567890",
                State = Guid.NewGuid(), 
                Address = "1234 Elm St, Springfield",
                AccountNumber = "987654321"
            };


            _mockSolicitorManager.Setup(manager => manager.EditSolicitorProfile(request, request.SolicitorId))
                .ReturnsAsync(true);

            // Act
            var response = await _solicitorService.EditSolicitorProfile(request);

            // Assert
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task EditSolicitorProfile_FailureInProfileEdit_ShouldReturnServiceErrorResponse()
        {
            // Arrange
            var request = new EditSolicitorProfileByLegalTeamRequest
            {
                SolicitorId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FirmName = "Doe and Associates",
                Email = "john.doe@example.com",
                PhoneNumber = "1234567890",
                State = Guid.NewGuid(),
                Address = "1234 Elm St, Springfield",
                AccountNumber = "987654321"
            };

            _mockSolicitorManager.Setup(manager => manager.EditSolicitorProfile(request, request.SolicitorId))
                .ReturnsAsync(false);

            // Act
            var response = await _solicitorService.EditSolicitorProfile(request);

            // Assert
            Assert.Equal(ResponseCodes.ServiceError, response.Code);
        }

        private class MockUserManager
        {
            public static Mock<UserManager<Domain.Entities.User.User>> CreateMockUserManager()
            {
                var userStoreMock = new Mock<IUserStore<Domain.Entities.User.User>>();
                return new Mock<UserManager<Domain.Entities.User.User>>(
                    userStoreMock.Object, null, null, null, null, null, null, null, null);
            }
        }
    }

}
