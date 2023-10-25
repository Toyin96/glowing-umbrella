using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class NotificationTemplatesTests
    {
        [Fact]
        public void GenerateNewRequestNotificationForSolicitor_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.GenerateNewRequestNotificationForSolicitor();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void NotifySolicitorOnRequestAssignment_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.NotifySolicitorOnRequestAssignment();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void OutstandingRequestNotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.OutstandingRequestNotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void RequestwithElapsedSLANotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.RequestwithElapsedSLANotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void RequestReturnedNotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.RequestReturnedNotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void ManualSolicitorAssignmentNotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.ManualSolicitorAssignmentNotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void RequestCompletedNotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.RequestCompletedNotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }

        [Fact]
        public void UnassignedRequestNotification_ReturnsNonEmptyString()
        {
            // Act
            var template = NotificationTemplates.UnassignedRequestNotification();

            // Assert
            Assert.NotNull(template);
            Assert.NotEmpty(template);
        }
    }
}
