using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Application.Models.Responses.Solicitor;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Tests.Controllers
{
    public class LegalPerfectionTeamsControllerTests
    {
        private readonly Mock<ISolicitorService> _solicitorServiceMock;
        private readonly LegalPerfectionTeamsController _controller;

        public LegalPerfectionTeamsControllerTests()
        {
            _solicitorServiceMock = new Mock<ISolicitorService>();
            _controller = new LegalPerfectionTeamsController(_solicitorServiceMock.Object);
        }

        [Fact]
        public async Task ManuallyAssignRequestToSolicitor_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ManuallyAssignRequestToSolicitorRequest();
            _solicitorServiceMock.Setup(x => x.ManuallyAssignRequestToSolicitor(request))
                .ReturnsAsync(new StatusResponse("Operation was successful"));

            // Act
            var result = await _controller.ManuallyAssignRequestToSolicitor(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<StatusResponse>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Operation was successful", response.Description);
        }

        [Fact]
        public async Task ViewMappedSolicitorsProfiles_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ViewSolicitorsBasedOnRegionRequestFilter();
            _solicitorServiceMock.Setup(x => x.ViewMappedSolicitorsProfiles(request))
                .ReturnsAsync(new ListResponse<SolicitorProfileResponseDto>("Operation was successful"));

            // Act
            var result = await _controller.ViewMappedSolicitorsProfiles(request);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ListResponse<SolicitorProfileResponseDto>>(okResult.Value);
            Assert.Equal("00", response.Code);
            Assert.Equal("Operation was successful", response.Description);
        }
    }
}
