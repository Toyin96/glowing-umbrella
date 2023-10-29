using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Security.Claims;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Application.Models.Responses.CSO;

namespace LegalSearch.Tests.Controllers
{
    public class SolicitorsControllerTests
    {
        private readonly Mock<ILegalSearchRequestService> _legalSearchRequestServiceMock;
        private readonly Mock<ISolicitorService> _solicitorServiceMock;
        private readonly SolicitorsController _controller;

        public SolicitorsControllerTests()
        {
            _legalSearchRequestServiceMock = new Mock<ILegalSearchRequestService>();
            _solicitorServiceMock = new Mock<ISolicitorService>();
            _controller = new SolicitorsController(_legalSearchRequestServiceMock.Object, _solicitorServiceMock.Object);
        }

        [Fact]
        public async Task AcceptRequest_ValidRequest_ReturnsOk()
        {
            // Populate the AcceptRequest object
            var acceptRequest = new AcceptRequest
            {
                RequestId = Guid.NewGuid(),
                SolicitorId = Guid.NewGuid() // Assuming you want to set this to a valid SolicitorId
            };

            // Mocking the response
            var statusResponse = new StatusResponse("Operation was successful", ResponseCodes.Success);

            // Set up user claims
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(nameof(ClaimType.UserId), userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

            // Setup the mock
            _legalSearchRequestServiceMock.Setup(x => x.AcceptLegalSearchRequest(acceptRequest))
                .ReturnsAsync(statusResponse);

            // Act
            var result = await _controller.AcceptRequest(acceptRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(statusResponse, response);

            // Verify the method was called
            _legalSearchRequestServiceMock.Verify(x => x.AcceptLegalSearchRequest(acceptRequest), Times.Once);
        }

        [Fact]
        public async Task RejectRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var rejectRequest = new RejectRequest { /* initialize with appropriate values */ };
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(nameof(ClaimType.UserId), userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

            var statusResponse = new StatusResponse("Operation was successful", ResponseCodes.Success);
            _legalSearchRequestServiceMock.Setup(x => x.RejectLegalSearchRequest(rejectRequest))
                .ReturnsAsync(statusResponse);

            // Act
            var result = await _controller.RejectRequest(rejectRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(statusResponse, response);

            _legalSearchRequestServiceMock.Verify(x => x.RejectLegalSearchRequest(rejectRequest), Times.Once);
        }

        [Fact]
        public async Task RequestAdditionalInformation_ValidRequest_ReturnsOk()
        {
            // Arrange
            var returnRequest = new ReturnRequest { /* initialize with appropriate values */ };
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(nameof(ClaimType.UserId), userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

            var statusResponse = new StatusResponse("Operation was successful", ResponseCodes.Success);
            _legalSearchRequestServiceMock.Setup(x => x.PushBackLegalSearchRequestForMoreInfo(returnRequest, It.IsAny<Guid>()))
                .ReturnsAsync(statusResponse);

            // Act
            var result = await _controller.RequestAdditionalInformation(returnRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(statusResponse, response);

            _legalSearchRequestServiceMock.Verify(x => x.PushBackLegalSearchRequestForMoreInfo(returnRequest, It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task SubmitRequestReport_ValidRequest_ReturnsOk()
        {
            // Arrange
            var submitRequest = new SubmitLegalSearchReport { /* initialize with appropriate values */ };
            var userId = Guid.NewGuid().ToString();
            var claims = new List<Claim> { new Claim(nameof(ClaimType.UserId), userId) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

            var statusResponse = new StatusResponse("Operation was successful", ResponseCodes.Success);
            _legalSearchRequestServiceMock.Setup(x => x.SubmitRequestReport(submitRequest, It.IsAny<Guid>()))
                .ReturnsAsync(statusResponse);

            // Act
            var result = await _controller.SubmitRequestReport(submitRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(statusResponse, response);

            _legalSearchRequestServiceMock.Verify(x => x.SubmitRequestReport(submitRequest, It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task GenerateRequestAnalyticsReport_Returns_SuccessResult()
        {
            // Arrange
            var controller = new SolicitorsController(_legalSearchRequestServiceMock.Object, _solicitorServiceMock.Object);

            // Mock the User object and its claims
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "sampleUserId"),
            new Claim(nameof(ClaimType.UserId), Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            // Set the user for the controller's HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            // Assign the mocked HttpContext to the controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            var request = new SolicitorRequestAnalyticsPayload
            {
                ReportFormatType = ReportFormatType.Excel
            };

            // Mock service response
            var mockResponse = new ObjectResponse<byte[]>("Success", ResponseCodes.Success)
            {
                Data = new byte[] { 1, 2, 3 } // Replace with appropriate test data
            };

            _legalSearchRequestServiceMock.Setup(x => x.GenerateRequestAnalyticsReportForSolicitor(It.IsAny<SolicitorRequestAnalyticsPayload>(), It.IsAny<Guid>()))
                                           .ReturnsAsync(mockResponse);

            // Act
            var result = await controller.GenerateRequestAnalyticsReport(request);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result.Result);
            var response = Assert.IsType<byte[]>(fileResult.FileContents);
            Assert.Equal(mockResponse.Data, response);
        }

        [Fact]
        public async Task ViewRequestAnalytics_Returns_SuccessResult()
        {
            // Arrange
            var controller = new SolicitorsController(_legalSearchRequestServiceMock.Object, _solicitorServiceMock.Object);

            // Mock the User object and its claims
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "sampleUserId"),
            new Claim(nameof(ClaimType.UserId), Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            // Set the user for the controller's HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            // Assign the mocked HttpContext to the controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            var request = new SolicitorRequestAnalyticsPayload
            {
                ReportFormatType = ReportFormatType.Excel
            };

            // Mock service response
            var mockResponse = new ObjectResponse<LegalSearchRootResponsePayload>("Success", ResponseCodes.Success)
            {
                Data = new LegalSearchRootResponsePayload
                {
                    LegalSearchRequests = new List<LegalSearchResponsePayload>
                    {
                        new LegalSearchResponsePayload
                        {
                            Id = Guid.NewGuid(),
                            RequestInitiator = "John Doe",
                            RequestType = "Sample Request Type",
                            CustomerAccountName = "Customer A",
                            RequestStatus = "Pending",
                            CustomerAccountNumber = "123456",
                            BusinessLocation = "Business Location A",
                            BusinessLocationId = Guid.NewGuid(),
                            RegistrationLocation = "Registration Location A",
                            RequestSubmissionDate = DateTime.Now,
                            RegistrationLocationId = Guid.NewGuid(),
                            RegistrationNumber = "REG123",
                            DateCreated = DateTime.Now,
                            DateDue = DateTime.Now.AddDays(7),
                            Solicitor = "Solicitor A",
                            ReasonOfCancellation = "Not applicable",
                            DateOfCancellation = null, // Set appropriate cancellation date if needed
                            RegistrationDate = DateTime.Now,
                            Region = "Region A",
                            RegionCode = Guid.NewGuid(),
                            Discussions = new List<DiscussionDto>
                            {
                                new DiscussionDto {  Conversation = "sample text 1" },
                                new DiscussionDto {  Conversation = "sample text 2" }
                            },
                            RegistrationDocuments = new List<RegistrationDocumentDto>
                            {
                                new RegistrationDocumentDto { FileName = "Document 1", FileType = "pdf", FileContent =  new byte[]{ 1, 2, 3} },
                                new RegistrationDocumentDto { FileName = "Document 2", FileType = "pdf", FileContent =  new byte[]{ 1, 2, 3}}
                            },
                            SupportingDocuments = new List<RegistrationDocumentDto>
                            {
                                new RegistrationDocumentDto { FileName = "Supporting Document 1" , FileType = "pdf", FileContent =  new byte[]{ 1, 2, 3}},
                                new RegistrationDocumentDto { FileName = "Supporting Document 2", FileType = "pdf", FileContent =  new byte[]{ 1, 2, 3} }
                            }
                        }
                    },
                }
            };

            _legalSearchRequestServiceMock.Setup(x => x.GetLegalRequestsForSolicitor(It.IsAny<SolicitorRequestAnalyticsPayload>(), It.IsAny<Guid>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await controller.ViewRequestAnalytics(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<LegalSearchRootResponsePayload>>(okResult.Value);
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(mockResponse, response);
        }

        [Fact]
        public async Task EditProfile_Returns_SuccessResult()
        {
            // Arrange
            var controller = new SolicitorsController(_legalSearchRequestServiceMock.Object, _solicitorServiceMock.Object);

            // Mock the User object and its claims
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "sampleUserId"),
            new Claim(nameof(ClaimType.UserId), Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            // Set the user for the controller's HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            // Assign the mocked HttpContext to the controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            var request = new EditSolicitorProfileByLegalTeamRequest
            {
                SolicitorId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FirmName = "Sample Firm",
                Email = "john.doe@example.com",
                PhoneNumber = "1234567890",
                State = Guid.NewGuid(),
                Address = "1234 Elm St, Some City, Some Country",
                AccountNumber = "ACC123456"
            };

            // Mock service response
            var mockResponse = new StatusResponse("Success", ResponseCodes.Success);
            _solicitorServiceMock.Setup(x => x.EditSolicitorProfile(It.IsAny<EditSolicitorProfileByLegalTeamRequest>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await controller.EditProfile(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(mockResponse, response);
        }

        [Fact]
        public async Task ViewProfile_Returns_SuccessResult()
        {
            // Arrange
            var controller = new SolicitorsController(_legalSearchRequestServiceMock.Object, _solicitorServiceMock.Object);

            // Mock the User object and its claims
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "sampleUserId"),
            new Claim(nameof(ClaimType.UserId), Guid.NewGuid().ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            // Set the user for the controller's HttpContext
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(user);

            // Assign the mocked HttpContext to the controller
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext.Object
            };

            // Mock service response
            var mockResponse = new ObjectResponse<SolicitorProfileDto>("Success", ResponseCodes.Success)
            {
                Data = new SolicitorProfileDto
                {
                    SolicitorId = Guid.NewGuid(),
                    FirstName = "John",
                    LastName = "Doe",
                    Firm = "Sample Firm",
                    SolicitorRegion = "South West",
                    SolicitorEmail = "john.doe@example.com",
                    SolicitorPhoneNumber = "1234567890",
                    SolicitorState = "Lagos",
                    Status = "Active",
                    SolicitorAddress = "1234 Elm St, Some City, Some Country",
                    BankAccountNumber = "ACC123456"
                }
            };
            _solicitorServiceMock.Setup(x => x.ViewSolicitorProfile(It.IsAny<Guid>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await controller.ViewProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<SolicitorProfileDto>>(okResult.Value);
            Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)okResult.StatusCode);
            Assert.Equal(ResponseCodes.Success, response.Code);
            Assert.Equal(mockResponse, response);
        }
    }
}
