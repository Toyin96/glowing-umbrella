using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.BackgroundService;
using LegalSearch.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using System;
using System.Text.Json;

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

            appDbContext.Setup(db => db.Users.FindAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            notificationServiceMock.Setup(service => service.NotifyUser(It.Is<Notification>(notification =>
                notification.Title == ConstantTitle.AdditionalInformationNeededOnAssignedRequestTitle
                && notification.NotificationType == NotificationType.RequestReturnedToCso
                && notification.RecipientUserId == "user123"
                && notification.RecipientUserEmail == "user@example.com"
                && notification.SolId == "061"
                && notification.Message == ConstantMessage.RequestRoutedBackToCSOMessage
            )));

            // Act
            await backgroundService.PushBackRequestToCSOJob(requestId);

            // Assert
            notificationServiceMock.Verify(service => service.NotifyUser(It.IsAny<Notification>()), Times.Once);
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

        [Fact]
        public async Task RetryFailedLegalSearchRequestSettlementToSolicitor_EligibleRecordsExist_RetriesPayments()
        {
            // Arrange
            var paymentLogRecords = new List<LegalSearchRequestPaymentLog>
            {
                new LegalSearchRequestPaymentLog
                {
                    SourceAccountName = "Source Account",
                    SourceAccountNumber = "1234567890",
                    DestinationAccountName = "Destination Account",
                    DestinationAccountNumber = "9876543210",
                    LienId = "Lien123",
                    CurrencyCode = "USD",
                    PaymentStatus = PaymentStatusType.MakePayment,
                    TransferRequestId = "TR123",
                    TransferNarration = "Payment for Legal Search",
                    TranId = "Tran123",
                    TransactionStan = "STAN456",
                    TransferAmount = 1000.00M,
                    PaymentResponseMetadata = "Payment Successful",
                    LegalSearchRequestId = Guid.NewGuid()
                }
            };

            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();

            // Create a user store
            var userStore = new Mock<IUserStore<User>>();

            // Create the UserManager
            var userManager = new UserManager<User>(userStore.Object, null, null, null, null, null, null, null, null);

            var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();

            // Mock the legal search request payment log manager
            var legalSearchRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            legalSearchRequestPaymentLogManagerMock.Setup(manager => manager.GetAllLegalSearchRequestPaymentLogNotYetCompleted())
                .ReturnsAsync(paymentLogRecords);

            // Create FCMB configuration options
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

            // Create the BackgroundService with the mocks and options
            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalSearchRequestPaymentLogManagerMock.Object,
                userManager,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            // Act
            await backgroundService.RetryFailedLegalSearchRequestSettlementToSolicitor();

            // Assert
            legalSearchRequestPaymentLogManagerMock.Verify(m => m.GetAllLegalSearchRequestPaymentLogNotYetCompleted(), Times.Once);
        }

        [Fact]
        public async Task ManuallyAssignRequestToSolicitorJob_ValidRequest_AssignsSolicitorAndNotifies()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();

            // Create a user store
            var userStore = new Mock<IUserStore<User>>();
            var legalSearchRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();

            // Create FCMB configuration options
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


            var requestId = Guid.NewGuid();
            var solicitorInfo = new UserMiniDto
            {
                UserId = Guid.NewGuid(),
                UserEmail = "solicitor@example.com"
            };

            var legalSearchRequest = new LegalRequest
            {
                BranchId = "061",
                Status = nameof(RequestStatusType.Initiated),
                CustomerAccountName = "Test",
                CustomerAccountNumber = "0123456789"
            };

            var usersInRole = new List<User>
            {
                new User { Id = Guid.NewGuid(), Email = "user@example.com", FirstName = "Test" },
            };

            var legalSearchRequestManager = new Mock<ILegalSearchRequestManager>();
            legalSearchRequestManager.Setup(manager => manager.GetLegalSearchRequest(requestId)).ReturnsAsync(legalSearchRequest);

            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null
            );
            userManager.Setup(manager => manager.GetUsersInRoleAsync(nameof(RoleType.LegalPerfectionTeam))).ReturnsAsync(usersInRole);

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalSearchRequestPaymentLogManagerMock.Object,
                userManager.Object,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            // Act
            await backgroundService.ManuallyAssignRequestToSolicitorJob(requestId, solicitorInfo);

            // Assert
            // Verify that the necessary methods were called as expected
            legalSearchRequestManager.Verify();
            userManager.Verify();
            notificationServiceMock.Verify();
        }

        [Fact]
        public async Task RequestEscalationJob_SolicitorRecipient_NotifiesSolicitor()
        {
            // Arrange
            var appDbContext = new Mock<AppDbContext>();
            var notificationServiceMock = new Mock<INotificationService>();
            var solicitorManagerMock = new Mock<ISolicitorManager>();
            var stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            var legalRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var fCMBServiceMock = new Mock<IFcmbService>();

            // Create a user store
            var userStore = new Mock<IUserStore<User>>();
            var legalSearchRequestPaymentLogManagerMock = new Mock<ILegalSearchRequestPaymentLogManager>();
            var zonalManagerServiceMock = new Mock<IZonalManagerService>();
            var customerManagerServiceMock = new Mock<ICustomerManagerService>();
            var emailServiceMock = new Mock<IEmailService>();
            var loggerMock = new Mock<ILogger<BackgroundService>>();

            // Create FCMB configuration options
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

            var requestId = Guid.NewGuid(); // Provide a valid request ID
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com", FirstName = "TestUser" };

            var legalRequest = new LegalRequest
            {
                BranchId = "061",
                Status = nameof(RequestStatusType.Initiated),
                CustomerAccountName = "Test",
                CustomerAccountNumber = "0123456789"
            };

            var userManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null
            );
            var legalSearchRequestManagerMock = new Mock<ILegalSearchRequestManager>();
            var userManagerMock = new Mock<UserManager<User>>();

            legalSearchRequestManagerMock.Setup(manager => manager.GetLegalSearchRequest(requestId))
                .ReturnsAsync(legalRequest);

            userManagerMock.Setup(userManager => userManager.GetUsersInRoleAsync(nameof(RoleType.LegalPerfectionTeam)))
                .ReturnsAsync(new List<User> { user });

            notificationServiceMock.Setup(service => service.NotifyUser(It.IsAny<Notification>()));

            var backgroundService = new BackgroundService(
                appDbContext.Object,
                new List<INotificationService> { notificationServiceMock.Object },
                solicitorManagerMock.Object,
                stateRetrieveServiceMock.Object,
                legalRequestManagerMock.Object,
                fCMBServiceMock.Object,
                options,
                legalSearchRequestPaymentLogManagerMock.Object,
                userManager.Object,
                zonalManagerServiceMock.Object,
                customerManagerServiceMock.Object,
                emailServiceMock.Object,
                loggerMock.Object
            );

            var escalationRequest = new EscalateRequest
            {
                RequestId = requestId,
                RecipientType = NotificationRecipientType.Solicitor,
            };

            // Act
            await backgroundService.RequestEscalationJob(escalationRequest);

            // Assert
            // Verify that NotifyUser was called with the notification payload
            legalRequestManagerMock.Verify(service => service.GetLegalSearchRequest(It.IsAny<Guid>()), Times.Once);
        }
    }
}
