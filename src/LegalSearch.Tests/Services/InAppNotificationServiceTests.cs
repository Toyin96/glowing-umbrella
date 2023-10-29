using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Services.Notification;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalSearch.Test.Services
{
    public class InAppNotificationServiceTests
    {
        [Fact]
        public async Task GetNotificationsForUser_ValidUser_ReturnsNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            notificationManager.Setup(x => x.GetPendingNotificationsForUser(userId.ToString(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<NotificationResponse> { new NotificationResponse { Message = "sample notification" } });

            userManager.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User { Id = userId, FirstName = "Test user" });

            userManager.Setup(u => u.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "UserRole" });

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.GetNotificationsForUser(userId.ToString());

            // Assert
            Assert.Equal(ResponseCodes.Success, result.Code);
            Assert.Equal("Operation was successful", result.Description);
            Assert.NotEmpty(result.Data);
        }

        [Fact]
        public async Task GetNotificationsForUser_InvalidUser_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            userManager.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null);

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.GetNotificationsForUser(userId.ToString());

            // Assert
            Assert.Equal(ResponseCodes.Forbidden, result.Code);
            Assert.Equal("Something went wrong. Please try again", result.Description);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task MarkAllNotificationsAsRead_Success_ReturnsSuccess()
        {
            // Arrange
            var userId = "testUserId";
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            notificationManager.Setup(n => n.MarkAllNotificationAsRead(userId))
                .ReturnsAsync(true);

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.MarkAllNotificationsAsRead(userId);

            // Assert
            Assert.Equal(ResponseCodes.Success, result.Code);
            Assert.Equal("Operation was successful", result.Description);
        }

        [Fact]
        public async Task MarkAllNotificationsAsRead_Failure_ReturnsBadRequest()
        {
            // Arrange
            var userId = "testUserId";
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            notificationManager.Setup(n => n.MarkAllNotificationAsRead(userId))
                .ReturnsAsync(false);

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.MarkAllNotificationsAsRead(userId);

            // Assert
            Assert.Equal(ResponseCodes.BadRequest, result.Code);
            Assert.Equal("Something went wrong. Please try again later", result.Description);
        }

        [Fact]
        public async Task MarkNotificationAsRead_Success_ReturnsSuccess()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            notificationManager.Setup(n => n.MarkNotificationAsRead(notificationId))
                .ReturnsAsync(true);

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.MarkNotificationAsRead(notificationId);

            // Assert
            Assert.Equal(ResponseCodes.Success, result.Code);
            Assert.Equal("Operation was successful", result.Description);
        }

        [Fact]
        public async Task MarkNotificationAsRead_Failure_ReturnsBadRequest()
        {
            // Arrange
            var notificationId = Guid.NewGuid();
            var userManager = MockUserManager();
            var notificationManager = MockNotificationManager();

            notificationManager.Setup(n => n.MarkNotificationAsRead(notificationId))
                .ReturnsAsync(false);

            var service = new InAppNotificationService(notificationManager.Object, userManager.Object);

            // Act
            var result = await service.MarkNotificationAsRead(notificationId);

            // Assert
            Assert.Equal(ResponseCodes.BadRequest, result.Code);
            Assert.Equal("Something went wrong. Please try again later", result.Description);
        }

        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<INotificationManager> MockNotificationManager()
        {
            return new Mock<INotificationManager>();
        }
    }
}
