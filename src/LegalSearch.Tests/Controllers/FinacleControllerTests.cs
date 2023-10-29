using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LegalSearch.Tests.Controllers
{
    public class FinacleControllerTests
    {
        private readonly Mock<ILegalSearchRequestService> _legalSearchRequestServiceMock;
        private readonly FinacleController _controller;

        public FinacleControllerTests()
        {
            _legalSearchRequestServiceMock = new Mock<ILegalSearchRequestService>();
            _controller = new FinacleController(_legalSearchRequestServiceMock.Object);
        }

        [Fact]
        public async Task CreateLegalRequest_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new FinacleLegalSearchRequest
            {
                BranchId = "SampleBranchId",
                CustomerAccountName = "SampleAccountName",
                CustomerAccountNumber = "SampleAccountNumber"
            };

            _legalSearchRequestServiceMock
                .Setup(x => x.CreateNewRequestFromFinacle(request))
                .ReturnsAsync(new StatusResponse("Operation was successful", ResponseCodes.Success));

            // Act
            var result = await _controller.CreateLegalRequest(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal(ResponseCodes.Success, response.Code);
        }

        [Fact]
        public async Task CreateLegalRequest_ServiceReturnsError_ReturnsBadRequest()
        {
            // Arrange
            var request = new FinacleLegalSearchRequest
            {
                BranchId = "SampleBranchId",
                CustomerAccountName = "SampleAccountName",
                CustomerAccountNumber = "SampleAccountNumber"
            };

            _legalSearchRequestServiceMock
                .Setup(x => x.CreateNewRequestFromFinacle(request))
                .ReturnsAsync(new StatusResponse("Error occurred", ResponseCodes.BadRequest));

            // Act
            var result = await _controller.CreateLegalRequest(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(badRequestResult.Value);
            Assert.Equal(ResponseCodes.BadRequest, response.Code);
        }
    }
}
