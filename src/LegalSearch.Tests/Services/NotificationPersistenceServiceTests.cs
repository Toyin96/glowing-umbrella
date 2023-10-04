﻿using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Infrastructure.Services.Notification;
using LegalSearch.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalSearch.Tests.Services
{
    public class NotificationPersistenceServiceTests
    {
        private Mock<INotificationManager> _mockNotificationManager;
        private Mock<UserManager<Domain.Entities.User.User>> _mockUserManager;
        private INotificationService _notificationService;

        public NotificationPersistenceServiceTests()
        {
            _mockNotificationManager = new Mock<INotificationManager>();
            _mockUserManager = MockUserManager.CreateMockUserManager<Domain.Entities.User.User>();
            _notificationService = new NotificationPersistenceService(_mockNotificationManager.Object, _mockUserManager.Object);
        }

        [Fact]
        public async Task NotifyUsersInRole_ValidInput_ShouldCallAddMultipleNotifications()
        {
            // Arrange
            var roleName = "TestRole";
            var notification = new Domain.Entities.Notification.Notification();

            // Act
            await _notificationService.NotifyUsersInRole(roleName, notification);

            // Assert
            _mockNotificationManager.Verify(x => x.AddMultipleNotifications(It.IsAny<List<Domain.Entities.Notification.Notification>>()), Times.Once);
        }

        [Fact]
        public async Task NotifyUser_ValidInput_ShouldCallAddMultipleNotifications()
        {
            // Arrange
            var initiatorUserId = Guid.NewGuid();
            var notification = new Domain.Entities.Notification.Notification();

            // Act
            await _notificationService.NotifyUser(initiatorUserId, notification);

            // Assert
            _mockNotificationManager.Verify(x => x.AddMultipleNotifications(It.IsAny<List<Domain.Entities.Notification.Notification>>()), Times.Once);
        }

        [Theory]
        [InlineData(Domain.Enums.Notification.NotificationType.AssignedToSolicitor)]
        [InlineData(Domain.Enums.Notification.NotificationType.OutstandingRequestAfter24Hours)]
        [InlineData(Domain.Enums.Notification.NotificationType.RequestReturnedToCso)]
        [InlineData(Domain.Enums.Notification.NotificationType.CompletedRequest)]
        [InlineData(Domain.Enums.Notification.NotificationType.UnAssignedRequest)]
        public async Task NotifyUsersInRole_WithValidNotificationType_CallsAddMultipleNotifications(Domain.Enums.Notification.NotificationType notificationType)
        {
            // Arrange
            var roleName = "TestRole";
            var notification = new Domain.Entities.Notification.Notification { NotificationType = notificationType };

            // Act
            await _notificationService.NotifyUsersInRole(roleName, notification);

            // Assert
            _mockNotificationManager.Verify(x => x.AddMultipleNotifications(It.IsAny<List<Domain.Entities.Notification.Notification>>()), Times.Once);
        }
    }
}
