using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace LegalSearch.Tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<IInAppNotificationService> _notificationServiceMock = new Mock<IInAppNotificationService>();
        private readonly NotificationsController _controller;

        public NotificationsControllerTests()
        {
            _controller = new NotificationsController(_notificationServiceMock.Object);
        }

        [Fact]
        public async Task MarkNotificationAsRead_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new UpdateNotificationRequest { NotificationId = Guid.NewGuid() };

            _notificationServiceMock.Setup(x => x.MarkNotificationAsRead(request.NotificationId))
                .ReturnsAsync(new StatusResponse("Operation was successful", ResponseCodes.Success));

            // Act
            var result = await _controller.MarkNotificationAsRead(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("Operation was successful", response.Description);
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task GetPendingNotificationsForUser_ValidRequest_ReturnsOk()
        {
            // Arrange
            var userId = "user123"; // Example user ID

            // Mocking the User.Claims
            var claims = new List<Claim>
            {
                new Claim(nameof(ClaimType.UserId), userId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            var notifications = new List<NotificationResponse>
            {
                new NotificationResponse
                {
                    NotificationId = Guid.NewGuid(),
                    Title = "New Request",
                    NotificationType = NotificationType.NewRequest,
                    RecipientUserId = "user123",
                    Message = "You have a new request.",
                    DateCreated = DateTime.UtcNow,
                    IsRead = false,
                    MetaData = "Additional metadata for the notification."
                },
            };

            _notificationServiceMock.Setup(x => x.GetNotificationsForUser(userId))
                .ReturnsAsync(new ListResponse<NotificationResponse>("Operation was successful") { Data = notifications });

            // Act
            var result = await _controller.GetPendingNotificationsForUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ListResponse<NotificationResponse>>(okResult.Value);
            Assert.Equal(notifications, response.Data);

            _notificationServiceMock.Verify(x => x.GetNotificationsForUser(userId), Times.Once);
            _notificationServiceMock.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task MarkAllNotificationAsRead_ValidRequest_ReturnsOk()
        {
            // Arrange
            var userId = "123";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _notificationServiceMock.Setup(x => x.MarkAllNotificationsAsRead(It.IsAny<string>()))
                .ReturnsAsync(new StatusResponse("Operation was successful", ResponseCodes.Success));

            // Act
            var result = await _controller.MarkAllNotificationAsRead();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("Operation was successful", response.Description);
            Assert.Equal(ResponseCodes.Success, response.Code);

            // Ensure the service method was called with the correct userId
            _notificationServiceMock.Verify(x => x.MarkAllNotificationsAsRead(It.IsAny<string>()), Times.Once);
            _notificationServiceMock.VerifyNoOtherCalls();
        }
    }
}
