using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.BackgroundService;
using LegalSearch.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;

namespace LegalSearch.Test.Services
{
    public class BackgroundServiceTests
    {
        [Fact]
        public async Task AssignRequestToSolicitorsJob_ValidRequest_AssignsAndRoutesRequest()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();
            var legalRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null); var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            var requestId = Guid.NewGuid(); // Provide a valid request ID

            // Configure the behavior of your mocked services here
            legalRequestManagerMock.Setup(manager => manager.GetLegalSearchRequest(requestId))
                .ReturnsAsync(new LegalRequest
                {
                    BranchId = "061",
                    Status = nameof(RequestStatusType.Initiated),
                    CustomerAccountName = "Test",
                    CustomerAccountNumber = "0123456789"
                });

            solicitorManagerMock.Setup(manager => manager.DetermineSolicitors(It.IsAny<LegalRequest>()))
                .ReturnsAsync(new List<SolicitorRetrievalResponse>
                {
                    new SolicitorRetrievalResponse
                    {
                        SolicitorId = Guid.NewGuid(),
                        SolicitorEmail = "solicitor@example.com"
                    }
                });

            // Act
            await backgroundService.AssignRequestToSolicitorsJob(requestId);

            // Assert
            legalRequestManagerMock.Verify(m => m.GetLegalSearchRequest(requestId), Times.Once);
        }

        [Fact]
        public async Task PushRequestToNextSolicitorInOrder_ValidRequest_AssignsAndRoutesRequest()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();
            var legalRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null); var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            var requestId = Guid.NewGuid(); // Provide a valid request ID

            // Configure the behavior of your mocked services here
            legalRequestManagerMock.Setup(manager => manager.GetLegalSearchRequest(requestId))
                .ReturnsAsync(new LegalRequest
                {
                    BranchId = "061",
                    Status = nameof(RequestStatusType.Initiated),
                    CustomerAccountName = "Test",
                    CustomerAccountNumber = "0123456789"
                });

            solicitorManagerMock.Setup(manager => manager.GetNextSolicitorInLine(requestId, It.IsAny<int>()))
                .ReturnsAsync(new SolicitorAssignment
                {
                    SolicitorId = Guid.NewGuid(),
                    SolicitorEmail = "solicitor@example.com"
                });

            // Act
            await backgroundService.PushRequestToNextSolicitorInOrder(requestId);

            // Assert
            legalRequestManagerMock.Verify(m => m.GetLegalSearchRequest(requestId), Times.Once);
            solicitorManagerMock.Verify(m => m.GetNextSolicitorInLine(requestId, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task NotificationReminderForUnAttendedRequestsJob_NotifiesSolicitorsOfRequestsDue()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();
            var legalRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null); var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            // Configure the behavior of your mocked services here
            solicitorManagerMock.Setup(manager => manager.GetUnattendedAcceptedRequestsForTheTimeFrame(It.IsAny<DateTime>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<Guid>
                {
                     Guid.NewGuid(),
                });

            // Act
            await backgroundService.NotificationReminderForUnAttendedRequestsJob();

            // Assert
            solicitorManagerMock.Verify(m => m.GetUnattendedAcceptedRequestsForTheTimeFrame(It.IsAny<DateTime>(), It.IsAny<bool>()));
        }

        [Fact]
        public async Task PushBackRequestToCSOJob_RequestExists_NotifiesCSO()
        {
            // Arrange
            var requestId = Guid.NewGuid(); // Provide a valid request ID
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", FirstName = "TestUser" };

            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();
            var legalRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null); var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            legalRequestManagerMock.Setup(manager => manager.GetLegalSearchRequest(requestId))
                .ReturnsAsync(new LegalRequest
                {
                    Id = requestId,
                    BranchId = "061",
                    CustomerAccountName = "Test",
                    CustomerAccountNumber = "1023456789",
                    Status = nameof(RequestStatusType.AssignedToLawyer),
                    InitiatorId = Guid.NewGuid(), 
                });

            appDbContext.Setup(db => db.Users.FindAsync("user123"))
                .ReturnsAsync(user);

            // Act
            await backgroundService.PushBackRequestToCSOJob(requestId);

            // Assert
            notificationServiceMock.Verify(service => service.NotifyUser(It.Is<Notification>(notification =>
                notification.Title == ConstantTitle.AdditionalInformationNeededOnAssignedRequestTitle
                && notification.NotificationType == NotificationType.RequestReturnedToCso
                && notification.RecipientUserId == "user123"
                && notification.RecipientUserEmail == "user@example.com"
                && notification.SolId == "061"
                && notification.Message == ConstantMessage.RequestRoutedBackToCSOMessage
            )), Times.Once);
        }

        [Fact]
        public async Task CheckAndRerouteRequestsJob_RequestsToReroute_RoutesRequests()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();
            var legalRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();

            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null); var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();
            var fcmbConfig = new FCMBConfig
            {
                BaseUrl = "Your BaseUrl",
                ClientId = "Your ClientId",
                SecretKey = "Your SecretKey",
                FrontendBaseUrl = "Your FrontendBaseUrl",
                SubscriptionKey = "Your SubscriptionKey",
                Password = "Your Password",
                SLAPeriod = "Your SLAPeriod",
                CurrencyCode = "Your CurrencyCode",
                LegalSearchAmount = "Your LegalSearchAmount",
                LegalSearchRemarks = "Your LegalSearchRemarks",
                LegalSearchPaymentRemarks = "Your LegalSearchPaymentRemarks",
                LegalSearchReasonCode = "Your LegalSearchReasonCode",
                AuthConfig = new AuthConfig
                {
                    AuthUrl = "Your AuthUrl",
                    AuthClientId = "Your AuthClientId",
                    AuthSecretKey = "Your AuthSecretKey"
                },
                EmailConfig = new EmailConfig
                {
                    EmailUrl = "Your EmailUrl",
                    SenderEmail = "Your SenderEmail",
                    SenderName = "Your SenderName"
                }
            };

            var options = Options.Create(fcmbConfig);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            var requestId = Guid.NewGuid();

            var legalRequest = new LegalRequest
            {
                Id = requestId,
                Status = RequestStatusType.LawyerAccepted.ToString(),
                CustomerAccountName = "Test",
                CustomerAccountNumber = "0123456789",
                BranchId = "061",
            };

            solicitorManagerMock.Setup(m => m.GetRequestsToReroute())
                .ReturnsAsync(new List<Guid> { requestId });

            legalRequestManagerMock.Setup(manager => manager.GetLegalSearchRequest(requestId))
            .ReturnsAsync(new LegalRequest
            {
                BranchId = "061",
                Status = nameof(RequestStatusType.Initiated),
                CustomerAccountName = "Test",
                CustomerAccountNumber = "0123456789"
            });

            solicitorManagerMock.Setup(m => m.GetCurrentSolicitorMappedToRequest(requestId, legalRequest.AssignedSolicitorId ?? Guid.Empty))
                .ReturnsAsync(new SolicitorAssignment
                {
                    Id = Guid.NewGuid(),
                    SolicitorEmail = "example@gmail.com",
                    SolicitorId = Guid.NewGuid(),
                    Order = 1,
                    AssignedAt = DateTime.Now,
                    IsCurrentlyAssigned = true,
                });

            // Act
            await backgroundService.CheckAndRerouteRequestsJob();

            // Assert
            solicitorManagerMock.Verify(m => m.GetRequestsToReroute(), Times.Once);
        }

    }
}
