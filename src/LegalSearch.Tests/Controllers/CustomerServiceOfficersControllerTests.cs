using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LegalSearch.Tests.Controllers
{
    public class CustomerServiceOfficersControllerTests
    {
        private readonly Mock<ILegalSearchRequestService> _legalSearchRequestServiceMock;
        private readonly CustomerServiceOfficersController _controller;

        public CustomerServiceOfficersControllerTests()
        {
            _legalSearchRequestServiceMock = new Mock<ILegalSearchRequestService>();
            _controller = new CustomerServiceOfficersController(_legalSearchRequestServiceMock.Object);
        }

        [Fact]
        public async Task CreateNewRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new LegalSearchRequest
            {
                RequestType = "Type A",
                BusinessLocation = Guid.NewGuid(),
                RegistrationLocation = Guid.NewGuid(),
                CustomerAccountName = "John Doe",
                CustomerAccountNumber = "1234567890",
                RegistrationNumber = "REG-2023-456",
                RegistrationDate = DateTime.UtcNow,
                AdditionalInformation = "Additional info about the request",
                RegistrationDocuments = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(), 0, 0, "registrationDocument1", "registrationDocument1.pdf"),
                    new FormFile(new MemoryStream(), 0, 0, "registrationDocument2", "registrationDocument2.pdf")
                },
                        SupportingDocuments = new List<IFormFile>
                {
                    new FormFile(new MemoryStream(), 0, 0, "supportingDocument1", "supportingDocument1.pdf"),
                    new FormFile(new MemoryStream(), 0, 0, "supportingDocument2", "supportingDocument2.pdf")
                }
            };
            var userId = "123";

            var expectedResponse = new StatusResponse("Operation was successful", ResponseCodes.Success);

            // Set up user claims
            var claims = new[]
            {
                new Claim(nameof(ClaimType.UserId), userId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            // Assign the principal to the controller
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _legalSearchRequestServiceMock
                .Setup(x => x.CreateNewRequest(request, userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateNewRequest(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResponse = Assert.IsType<StatusResponse>(okResult.Value);

            Assert.Equal(expectedResponse.Code, actualResponse.Code);
            Assert.Equal(expectedResponse.Description, actualResponse.Description);
        }


        [Fact]
        public async Task UpdateFinacleRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var updateRequest = new UpdateFinacleLegalRequest
            {
                RequestId = Guid.NewGuid(),
                RequestType = "SampleRequestType",
                BusinessLocation = Guid.NewGuid(),
                RegistrationLocation = Guid.NewGuid(),
                CustomerAccountName = "SampleAccountName",
                CustomerAccountNumber = "SampleAccountNumber",
                RegistrationNumber = "SampleRegistrationNumber",
                RegistrationDate = DateTime.Now,
                AdditionalInformation = "SampleAdditionalInfo",
                RegistrationDocuments = new List<IFormFile> { /* Initialize with appropriate IFormFile instances */ },
                SupportingDocuments = new List<IFormFile> { /* Initialize with appropriate IFormFile instances */ }
            };

            var userId = "123";

            // Mocking user claims
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(nameof(ClaimType.UserId), userId)
            }));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            _legalSearchRequestServiceMock
                .Setup(x => x.UpdateFinacleRequestByCso(It.IsAny<UpdateFinacleLegalRequest>(), userId))
                .ReturnsAsync(new StatusResponse("Operation was successful", ResponseCodes.Success));

            // Act
            var result = await _controller.UpdateFinacleRequest(updateRequest);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task PerformNameInquiryOnAccount_ValidAccountNumber_ReturnsOk()
        {
            // Arrange
            var accountNumber = "1234567890";

            var mockGetAccountInquiryResponse = new GetAccountInquiryResponse
            {
                Data = new GetAccountInquiryResponseData
                {
                    CustomerId = "123456",
                    AccountStatus = "Active",
                    RCNo = "RC12345",
                    FrezCode = "FREZ",
                    AccountNumber = "1234567890",
                    Currency = "USD",
                    LienAmount = "0.00",
                    FreezeReason = "Not Frozen",
                    AvailableBalance = 5000.00m,
                    LedgerBalance = 5500.00m,
                    TotalCredit = 10000.00m,
                    TotalDebit = 5000.00m,
                    AccountName = "John Doe",
                    MobileNumber = "1234567890",
                    EmailAddress = "john.doe@example.com",
                    Address1 = "123 Main St, Cityville",
                    Sex = "Male",
                    AccountOfficerCode = "AO123",
                    EmployerId = "EMP123",
                    IsMinor = "No",
                    PassportNumber = "AB123456",
                    BranchCode = "BR123",
                    DateOfBirth = "1980-01-01",
                    SchemeCode = "SCHEME123",
                    SbuCode = "SBU123",
                    BrokerCode = "BROKER123",
                    LegalSearchAmount = 100.00m
                },
                Code = "00",
                Description = "Account inquiry successful"
            };


            _legalSearchRequestServiceMock.Setup(x => x.PerformNameInquiryOnAccount(accountNumber))
                .ReturnsAsync(new ObjectResponse<GetAccountInquiryResponse>("Operation was successful", ResponseCodes.Success) { Data = mockGetAccountInquiryResponse});

            // Act
            var result = await _controller.PerformNameInquiryOnAccount(accountNumber);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetFinacleRequests_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new GetFinacleRequest();
            var solId = "CSO123";

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
            new Claim(nameof(ClaimType.SolId), solId)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var mockResponse = new ListResponse<FinacleLegalSearchResponsePayload>("Operation was successful", ResponseCodes.Success)
            {
                Data = new List<FinacleLegalSearchResponsePayload>
                {
                    new FinacleLegalSearchResponsePayload
                    {
                        RequestId = Guid.NewGuid(),
                        CustomerAccountName = "John Doe",
                        RequestStatus = "Pending",
                        AccountBranchName = "Main Branch",
                        CustomerAccountNumber = "1234567890",
                        DateCreated = DateTime.Now
                    },
                    new FinacleLegalSearchResponsePayload
                    {
                        RequestId = Guid.NewGuid(),
                        CustomerAccountName = "Jane Smith",
                        RequestStatus = "Completed",
                        AccountBranchName = "Branch 1",
                        CustomerAccountNumber = "0987654321",
                        DateCreated = DateTime.Now.AddHours(-1)
                    }
                },
                Code = ResponseCodes.Success,
                Description = "Operation was successful"
            };


            _legalSearchRequestServiceMock.Setup(x => x.GetFinacleLegalRequestsForCso(request, solId))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.GetFinacleRequests(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task ViewRequestAnalytics_ValidRequest_ReturnsOk()
        {
            // Arrange
            var csoDashboardAnalyticsRequest = new StaffDashboardAnalyticsRequest();

            // Mocking the response payload
            var mockPayload = new StaffRootResponsePayload
            {
                // For simplicity, I'll populate with some dummy data
                TotalRequests = 100,
                PendingRequests = 30,
                CompletedRequests = 70
            };

            // Mocking the response
            var mockResponse = new ObjectResponse<StaffRootResponsePayload>("Operation was successful")
            {
                Data = mockPayload,
                Code = "00",
                Description = "Success"
            };

            // Setup the mock to return the mockResponse
            _legalSearchRequestServiceMock.Setup(x => x.GetLegalRequestsForStaff(csoDashboardAnalyticsRequest))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _controller.ViewRequestAnalytics(csoDashboardAnalyticsRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responsePayload = Assert.IsType<ObjectResponse<StaffRootResponsePayload>>(okResult.Value);

            // Assert the response payload data
            Assert.NotNull(responsePayload.Data);
            Assert.Equal(mockPayload.TotalRequests, responsePayload.Data.TotalRequests);
            Assert.Equal(mockPayload.PendingRequests, responsePayload.Data.PendingRequests);
            Assert.Equal(mockPayload.CompletedRequests, responsePayload.Data.CompletedRequests);
        }
    }
}
