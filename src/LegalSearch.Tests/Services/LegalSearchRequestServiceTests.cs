//using LegalSearch.Application.Interfaces.FCMBService;
//using LegalSearch.Application.Interfaces.LegalSearchRequest;
//using LegalSearch.Application.Interfaces.Notification;
//using LegalSearch.Application.Interfaces.User;
//using LegalSearch.Application.Models.Constants;
//using LegalSearch.Application.Models.Requests.Solicitor;
//using LegalSearch.Application.Models.Requests;
//using LegalSearch.Application.Models.Responses;
//using LegalSearch.Infrastructure.Services.LegalSearchService;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using LegalSearch.Domain.Entities.User;
//using LegalSearch.Domain.Entities.LegalRequest;
//using LegalSearch.Domain.Enums.LegalRequest;

//namespace LegalSearch.Test.Services
//{
//    public class LegalSearchRequestServiceTests
//    {
//        [Fact]
//        public async Task AcceptLegalSearchRequest_ValidRequest_ReturnsSuccessResponse()
//        {
//            // Arrange
//            var loggerMock = new Mock<ILogger<LegalSearchRequestService>>();
//            var fcmbServiceMock = new Mock<IFcmbService>();
//            var userManagerMock = new Mock<UserManager<User>>(new Mock<IUserStore<User>>().Object, null, null, null, null, null, null, null, null);
//            var legalSearchRequestManagerMock = new Mock<ILegalSearchRequestManager>();
//            var solicitorAssignmentManagerMock = new Mock<ISolicitorAssignmentManager>();
//            var notificationServiceMock = new List<Mock<INotificationService>>();
//            var optionsMock = new Mock<IOptions<FCMBConfig>>();

//            var legalSearchRequestService = new LegalSearchRequestService(
//                loggerMock.Object, fcmbServiceMock.Object, userManagerMock.Object,
//                legalSearchRequestManagerMock.Object, solicitorAssignmentManagerMock.Object,
//                notificationServiceMock.Select(x => x.Object), optionsMock.Object
//            );

//            var request = new AcceptRequest
//            {
//                RequestId = Guid.NewGuid(),
//                SolicitorId = Guid.NewGuid()
//            };

//            // Mock dependencies as needed
//            var legalSearchRequest = new LegalSearchRequest(); // Create a sample LegalSearchRequest object

//            legalSearchRequestManagerMock.Setup(m => m.FetchAndValidateRequest(request.RequestId, request.SolicitorId, ActionType.AcceptRequest))
//                .ReturnsAsync((legalSearchRequest, "00"));

//            // Act
//            var response = await legalSearchRequestService.AcceptLegalSearchRequest(request);

//            // Assert
//            // Perform your assertions on the response
//            Assert.NotNull(response);
//            // Add more specific assertions as needed.

//            // Verify that expected methods were called on mocks
//            legalSearchRequestManagerMock.Verify(m => m.UpdateLegalSearchRequest(It.IsAny<LegalSearchRequest>()), Times.Once);
//        }

//        [Fact]
//        public async Task CreateNewRequest_ValidRequest_ReturnsSuccessResponse()
//        {
//            // Arrange
//            var loggerMock = new Mock<ILogger<LegalSearchRequestService>>();
//            var fcmbServiceMock = new Mock<IFcmbService>();
//            var userManagerMock = new Mock<UserManager<User>>();
//            var legalSearchRequestManagerMock = new Mock<ILegalSearchRequestManager>();
//            var solicitorAssignmentManagerMock = new Mock<ISolicitorAssignmentManager>();
//            var notificationServiceMock = new Mock<IEnumerable<INotificationService>>();
//            var optionsMock = new Mock<IOptions<FCMBConfig>>();

//            // Mocking the behavior of dependencies
//            userManagerMock.Setup(um => um.FindByIdAsync(It.IsAny<string>()))
//                .ReturnsAsync(new User { FirstName = "Test"}); // Mock that user is found

//            fcmbServiceMock.Setup(fcmb => fcmb.MakeAccountInquiry(It.IsAny<string>()))
//                .ReturnsAsync(new GetAccountInquiryResponse { Code = "00" }); // Mock a successful account inquiry

//            fcmbServiceMock.Setup(fcmb => fcmb.AddLien(It.IsAny<AddLienToAccountRequest>()))
//                .ReturnsAsync(new AddLienToAccountResponse { Code = "00" }); // Mock a successful lien addition

//            legalSearchRequestManagerMock.Setup(mgr => mgr.AddNewLegalSearchRequest(It.IsAny<LegalRequest>()))
//                .ReturnsAsync(true); // Mock a successful request creation

//            var service = new LegalSearchRequestService(
//                loggerMock.Object,
//                fcmbServiceMock.Object,
//                userManagerMock.Object,
//                legalSearchRequestManagerMock.Object,
//                solicitorAssignmentManagerMock.Object,
//                notificationServiceMock.Object,
//                optionsMock.Object
//            );

//            // Act
//            var legalRequest = new LegalSearchRequest
//            {
//                RequestType = nameof(RequestType.BusinessName),
//                RegistrationNumber = "0664641",
//                RegistrationDocuments = null,
//                SupportingDocuments = null,
//                CustomerAccountName = "Test",
//                CustomerAccountNumber = "0123456789",
//            };
//            var result = await service.CreateNewRequest(legalRequest, "userId");

//            // Assert
//            Assert.Equal(ResponseCodes.Success, result.Code);
//            Assert.Equal("Request created successfully", result.Description);
//        }
//    }
//}
