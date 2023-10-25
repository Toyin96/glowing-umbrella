using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace LegalSearch.Test.Infrastructure.Managers
{
    public class NotificationManagerTests
    {
        [Fact]
        public async Task AddMultipleNotifications_ReturnsTrueOnSuccess()
        {
            // Arrange
            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("TestDatabase1"))
                .BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationManager = new NotificationManager(dbContext, serviceProvider);

                var notifications = new List<Notification>
                {
                    new Notification
                    {
                        Title = "Sample Notification1",
                        NotificationType = NotificationType.ManualSolicitorAssignment,
                        RecipientRole = RoleType.Cso.ToString(),
                        IsRead = false
                    },
                    new Notification
                    {
                        Title = "Sample Notification2",
                        NotificationType = NotificationType.RequestWithElapsedSLA,
                        RecipientRole = RoleType.Cso.ToString(),
                        IsRead = false
                    }
                };

                // Act
                var result = await notificationManager.AddMultipleNotifications(notifications);

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public async Task AddNotification_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var notificationManager = new NotificationManager(dbContext, Mock.Of<IServiceProvider>());

                var notification = new Notification
                {
                    Title = "Sample Notification",
                    NotificationType = NotificationType.ManualSolicitorAssignment,
                    RecipientRole = RoleType.Cso.ToString(),
                    IsRead = false
                };

                // Act
                var result = await notificationManager.AddNotification(notification);

                // Assert
                Assert.True(result);

                // Verify that the notification was added to the in-memory database
                Assert.Equal(1, dbContext.Notifications.Count());
            }
        }


        [Fact]
        public async Task GetPendingNotificationsForRole_ReturnsFilteredNotifications()
        {
            // Arrange
            var role = RoleType.Cso.ToString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase3")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var notificationManager = new NotificationManager(dbContext, Mock.Of<IServiceProvider>());

                var notifications = new List<Notification>
                {
                    new Notification
                    {
                        Title = "Sample Notification1",
                        NotificationType = NotificationType.ManualSolicitorAssignment,
                        RecipientRole = role,
                        IsRead = false,
                        IsBroadcast = true
                    },
                    new Notification
                    {
                        Title = "Sample Notification2",
                        NotificationType = NotificationType.RequestWithElapsedSLA,
                        RecipientRole = RoleType.Solicitor.ToString(),
                        IsRead = false,
                        IsBroadcast = true
                    }
                };

                dbContext.Notifications.AddRange(notifications);
                dbContext.SaveChanges(); // Simulate adding notifications to the in-memory database

                // Act
                var result = await notificationManager.GetPendingNotificationsForRole(role);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(1, result.Count());
                Assert.Equal("Sample Notification1", result.Single().Title);
            }
        }

        [Fact]
        public async Task GetPendingNotificationsForUser_ReturnsFilteredNotifications()
        {
            // Arrange
            var userId = "SampleUserId";
            var role = RoleType.Cso.ToString();
            var solId = "SampleSolId";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase4")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var notificationManager = new NotificationManager(dbContext, Mock.Of<IServiceProvider>());

                var notifications = new List<Notification>
                {
                    new Notification
                    {
                        Title = "Sample Notification1",
                        NotificationType = NotificationType.ManualSolicitorAssignment,
                        RecipientUserId = userId,
                        IsRead = false,
                        IsBroadcast = true
                    },
                    new Notification
                    {
                        Title = "Sample Notification2",
                        NotificationType = NotificationType.RequestWithElapsedSLA,
                        RecipientRole = role,
                        IsRead = false,
                        IsBroadcast = true,
                        SolId = solId
                    }
                };

                dbContext.Notifications.AddRange(notifications);
                dbContext.SaveChanges(); // Simulate adding notifications to the in-memory database

                var notificationResponses = notifications
                    .Where(n => n.IsBroadcast && n.RecipientRole == role && !n.IsRead && n.SolId == solId)
                    .Select(x => new NotificationResponse
                    {
                        NotificationId = x.Id,
                        Title = x.Title,
                        NotificationType = x.NotificationType,
                        RecipientUserId = x.RecipientUserId,
                        Message = x.Message,
                        DateCreated = x.CreatedAt,
                        IsRead = x.IsRead,
                        MetaData = x.MetaData
                    })
                    .OrderByDescending(x => x.DateCreated)
                    .ToList();

                // Act
                var result = await notificationManager.GetPendingNotificationsForUser(userId, role, solId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(notificationResponses.Count, result.Count());
            }
        }

        [Fact]
        public async Task MarkAllNotificationAsRead_ReturnsTrueOnSuccess()
        {
            // Arrange
            var userId = "SampleUserId";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase5")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var notificationManager = new NotificationManager(dbContext, Mock.Of<IServiceProvider>());

                var notifications = new List<Notification>
                {
                    new Notification { RecipientUserId = userId, IsRead = false },
                    new Notification { RecipientUserId = userId, IsRead = false }
                };

                dbContext.Notifications.AddRange(notifications);
                dbContext.SaveChanges(); // Simulate adding notifications to the in-memory database

                // Act
                var result = await notificationManager.MarkAllNotificationAsRead(userId);

                // Assert
                Assert.True(result);
                Assert.All(notifications, n => Assert.True(n.IsRead));
            }
        }



        [Fact]
        public async Task MarkNotificationAsRead_ReturnsTrueOnSuccess()
        {
            // Arrange
            var notificationId = Guid.NewGuid();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase5")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var notificationManager = new NotificationManager(dbContext, Mock.Of<IServiceProvider>());

                var notifications = new List<Notification>
                {
                    new Notification { IsRead = false, Id = notificationId },
                    new Notification { IsRead = false }
                };

                dbContext.Notifications.AddRange(notifications);
                dbContext.SaveChanges(); // Simulate adding notifications to the in-memory database

                // Act
                var result = await notificationManager.MarkNotificationAsRead(notificationId);

                // Assert
                Assert.True(result);
                Assert.True(notifications[0].IsRead);
            }
        }
    }
}
