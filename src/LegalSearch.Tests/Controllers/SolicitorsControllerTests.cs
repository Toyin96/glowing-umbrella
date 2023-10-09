using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

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
    }
}
