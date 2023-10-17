using DocumentFormat.OpenXml.EMMA;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Application.Models.Responses.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;

namespace LegalSearch.Tests.Controllers
{
    public class CommonsControllerTests
    {
        [Fact]
        public async Task EscalateRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new EscalateRequest { RecipientType = Domain.Enums.Role.NotificationRecipientType.Solicitor, RequestId = Guid.NewGuid() };
            var mockLegalSearchRequestService = new Mock<ILegalSearchRequestService>();
            var mockSolicitorService = new Mock<ISolicitorService>();
            mockLegalSearchRequestService.Setup(service => service.EscalateRequest(It.IsAny<EscalateRequest>()))
                                         .ReturnsAsync(new Fcmb.Shared.Models.Responses.StatusResponse("Operation is successful", ResponseCodes.Success));
            var controller = new CommonsController(mockLegalSearchRequestService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.EscalateRequest(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Operation is successful", response.Description);
        }

        [Fact]
        public async Task UpdateRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new UpdateRequest
            {
                RequestId = Guid.NewGuid(),
                RequestType = "Corporate",
                CurrentRegistrationDocuments = null,
                CurrentSupportingDocuments = null,
                CustomerAccountName = "John Doe",
                CustomerAccountNumber = "0123456789",
                RegistrationDocuments = null,
                RegistrationNumber = "100001",
                SupportingDocuments = null
            };
            var mockLegalSearchRequestService = new Mock<ILegalSearchRequestService>();
            mockLegalSearchRequestService.Setup(service => service.UpdateRequestByStaff(It.IsAny<UpdateRequest>()))
                                         .ReturnsAsync(new StatusResponse("Success", ResponseCodes.Success));
            var mockSolicitorService = new Mock<ISolicitorService>();

            var controller = new CommonsController(mockLegalSearchRequestService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.UpdateRequest(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Success", response.Description);
        }

        [Fact]
        public async Task ViewRequestAnalytics_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new StaffDashboardAnalyticsRequest { };
            var mockLegalSearchRequestService = new Mock<ILegalSearchRequestService>();
            var mockRootResponse = new StaffRootResponsePayload
            {
                LegalSearchRequests = new List<LegalSearchResponsePayload>
            {
                new LegalSearchResponsePayload
                {
                    Id = Guid.NewGuid(),
                    RequestInitiator = "John Doe",
                    RequestType = "Type A",
                    CustomerAccountName = "Customer 1",
                    RequestStatus = "Pending",
                    CustomerAccountNumber = "0123456789",
                    BusinessLocation = Guid.NewGuid().ToString(),
                    Region = Guid.NewGuid().ToString(),
                    RegistrationLocation = Guid.NewGuid().ToString(),
                    BusinessLocationId = Guid.NewGuid(),
                    RegistrationLocationId = Guid.NewGuid(),
                    RegistrationNumber = Guid.NewGuid().ToString(),
                    ReasonOfCancellation = string.Empty
                },
                new LegalSearchResponsePayload
                {
                    Id = Guid.NewGuid(),
                    RequestInitiator = "Jane Smith",
                    RequestType = "Type B",
                    CustomerAccountName = "Customer 2",
                    RequestStatus = "Completed",
                    CustomerAccountNumber = "132745874",
                    BusinessLocation = Guid.NewGuid().ToString(),
                    Region = Guid.NewGuid().ToString(),
                    RegistrationLocation = Guid.NewGuid().ToString(),
                    BusinessLocationId = Guid.NewGuid(),
                    RegistrationLocationId = Guid.NewGuid(),
                    RegistrationNumber = Guid.NewGuid().ToString(),
                    ReasonOfCancellation = string.Empty
                }
            },
                RequestsCountBarChart = new List<MonthlyRequestData>
                {
                    new MonthlyRequestData { Name = "January", New = 10, Comp = 5 },
                    new MonthlyRequestData { Name = "February", New = 15, Comp = 8 }
                },
                PendingRequests = 20,
                CompletedRequests = 15,
                OpenRequests = 5,
                AverageProcessingTime = "2 days",
                TotalRequests = 50,
                WithinSLACount = 40,
                ElapsedSLACount = 10,
                Within3HoursToSLACount = 15,
                RequestsWithLawyersFeedbackCount = 8
            };

            var mockResponse = new ObjectResponse<StaffRootResponsePayload>("Success", ResponseCodes.Success) { Data = mockRootResponse };
            mockLegalSearchRequestService.Setup(service => service.GetLegalRequestsForStaff(It.IsAny<StaffDashboardAnalyticsRequest>()))
                                         .ReturnsAsync(mockResponse);

            var mockSolicitorService = new Mock<ISolicitorService>();
            var controller = new CommonsController(mockLegalSearchRequestService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.ViewRequestAnalytics(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
            Assert.Equal(mockResponse, okResult.Value);
        }


        [Fact]
        public async Task CancelRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CancelRequest { Reason = "Testing purpose", RequestId = Guid.NewGuid() };
            var mockLegalSearchRequestService = new Mock<ILegalSearchRequestService>();
            mockLegalSearchRequestService.Setup(service => service.CancelLegalSearchRequest(It.IsAny<CancelRequest>()))
                                         .ReturnsAsync(new StatusResponse("Operation is successful", ResponseCodes.Success));
            var mockSolicitorService = new Mock<ISolicitorService>();

            var controller = new CommonsController(mockLegalSearchRequestService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.CancelRequest(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Operation is successful", response.Description);
        }

        [Fact]
        public async Task ViewSolicitors_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ViewSolicitorsRequestFilter { /* fill with valid data */ };
            var mockSolicitorService = new Mock<ISolicitorService>();
            mockSolicitorService.Setup(service => service.ViewSolicitors(It.IsAny<ViewSolicitorsRequestFilter>()))
                                .ReturnsAsync(new ListResponse<Application.Models.Responses.Solicitor.SolicitorProfileDto>("Success", ResponseCodes.Success)
                                {
                                    Data = new List<Application.Models.Responses.Solicitor.SolicitorProfileDto>
                                    {
                                        new Application.Models.Responses.Solicitor.SolicitorProfileDto
                                        {
                                            SolicitorId = Guid.NewGuid(),
                                            FirstName = "John",
                                            LastName = "Doe",
                                            Firm = null,
                                            SolicitorEmail = "sample_user@example.com",
                                            SolicitorPhoneNumber = "+2349043827456",
                                            BankAccountNumber = "0123456789",
                                            SolicitorState = string.Empty,
                                            SolicitorRegion = null,
                                            SolicitorStateId = Guid.NewGuid(),
                                            Status = "Active",
                                            SolicitorAddress = ""
                                        },
                                    },
                                    Total = 1,
                                    Code = ResponseCodes.Success,
                                });

            var mockLegalSearchService = new Mock<ILegalSearchRequestService>();

            var controller = new CommonsController(mockLegalSearchService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.ViewSolicitors(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ListResponse<SolicitorProfileDto>>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Success", response.Description);
        }

        [Fact]
        public async Task EditProfile_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new EditSolicitorProfileByLegalTeamRequest
            {
                SolicitorId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                FirmName = string.Empty,
                Email = "sample_user@example.com",
                PhoneNumber = "+2349043827456",
                AccountNumber = "0123456789",
                State = Guid.NewGuid(),
                Address = ""
            };
            var mockSolicitorService = new Mock<ISolicitorService>();
            var mockLegalSearchService = new Mock<ILegalSearchRequestService>();
            mockSolicitorService.Setup(service => service.EditSolicitorProfile(It.IsAny<EditSolicitorProfileByLegalTeamRequest>()))
                                .ReturnsAsync(new StatusResponse("Success", ResponseCodes.Success));
            var controller = new CommonsController(mockLegalSearchService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.EditProfile(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Success", response.Description);
        }

        [Fact]
        public async Task ActivateOrDeactivateSolicitor_ValidRequest_ReturnsOk()
        {
            // Arrange
            var mockSolicitorService = new Mock<ISolicitorService>();
            var mockLegalSearchRequestService = new Mock<ILegalSearchRequestService>();

            // Mock the service method and specify the response
            mockSolicitorService.Setup(service => service.ActivateOrDeactivateSolicitor(It.IsAny<ActivateOrDeactivateSolicitorRequest>()))
                                .ReturnsAsync(new StatusResponse("Success", ResponseCodes.Success));

            var controller = new CommonsController(mockLegalSearchRequestService.Object, mockSolicitorService.Object);

            // Act
            var result = await controller.ActivateOrDeactivateSolicitor(Guid.NewGuid().ToString(), Domain.Enums.User.ProfileStatusActionType.Activate);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var statusResponse = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", statusResponse.Code);
            Assert.Equal("Success", statusResponse.Description);
        }

        [Fact]
        public async Task GenerateRequestAnalyticsReportForStaff_Success_ReturnsFileData()
        {
            // Arrange
            var mockService = new Mock<ILegalSearchRequestService>();
            var expectedFileData = new byte[] { 1, 2, 3 };  // Sample byte array representing the file content

            // Mock the service call
            mockService
                .Setup(service => service.GenerateRequestAnalyticsReportForStaff(It.IsAny<StaffDashboardAnalyticsRequest>()))
                .ReturnsAsync(new ObjectResponse<byte[]>("")
                {
                    Code = ResponseCodes.Success,
                    Data = expectedFileData
                });

            // Act
            var result = await mockService.Object.GenerateRequestAnalyticsReportForStaff(new StaffDashboardAnalyticsRequest());

            // Assert
            Assert.Equal(ResponseCodes.Success, result.Code);
            Assert.Equal(expectedFileData, result.Data);
        }

        [Fact]
        public async Task GenerateRequestAnalyticsReportForSolicitor_Success_ReturnsFileData()
        {
            // Arrange
            var mockService = new Mock<ILegalSearchRequestService>();
            var expectedFileData = new byte[] { 1, 2, 3 };  // Sample byte array representing the file content

            // Mock the service call
            mockService
                .Setup(service => service.GenerateRequestAnalyticsReportForSolicitor(It.IsAny<SolicitorRequestAnalyticsPayload>(), It.IsAny<Guid>()))
                .ReturnsAsync(new ObjectResponse<byte[]>("Operation was successful")
                {
                    Code = ResponseCodes.Success,
                    Data = expectedFileData
                });

            // Act
            var result = await mockService.Object.GenerateRequestAnalyticsReportForSolicitor(new SolicitorRequestAnalyticsPayload
            {
                ReportFormatType = ReportFormatType.Excel
            }, Guid.NewGuid());

            // Assert
            Assert.Equal(ResponseCodes.Success, result.Code);
            Assert.Equal(expectedFileData, result.Data);
        }

        [Fact]
        public async Task GenerateRequestAnalyticsReportForStaff_Error_ReturnsError()
        {
            // Arrange
            var mockService = new Mock<ILegalSearchRequestService>();

            // Mock the service call to return an error response
            mockService
                .Setup(service => service.GenerateRequestAnalyticsReportForStaff(It.IsAny<StaffDashboardAnalyticsRequest>()))
                .ReturnsAsync(new ObjectResponse<byte[]>("An error occurred while generating the report.", ResponseCodes.ServiceError));

            // Act
            var result = await mockService.Object.GenerateRequestAnalyticsReportForStaff(new StaffDashboardAnalyticsRequest());

            // Assert
            Assert.Equal(ResponseCodes.ServiceError, result.Code);
            Assert.Equal("An error occurred while generating the report.", result.Description);
        }

        [Fact]
        public async Task GenerateRequestAnalyticsReportForSolicitor_Error_ReturnsError()
        {
            // Arrange
            var mockService = new Mock<ILegalSearchRequestService>();

            // Mock the service call to return an error response
            mockService
                .Setup(service => service.GenerateRequestAnalyticsReportForSolicitor(It.IsAny<SolicitorRequestAnalyticsPayload>(), It.IsAny<Guid>()))
                .ReturnsAsync(new ObjectResponse<byte[]>("An error occurred while generating the report.", ResponseCodes.ServiceError));

            // Act
            var result = await mockService.Object.GenerateRequestAnalyticsReportForSolicitor(new SolicitorRequestAnalyticsPayload
            {
                ReportFormatType = ReportFormatType.Excel
            }, Guid.NewGuid());

            // Assert
            Assert.Equal(ResponseCodes.ServiceError, result.Code);
            Assert.Equal("An error occurred while generating the report.", result.Description);
        }
    }
}
