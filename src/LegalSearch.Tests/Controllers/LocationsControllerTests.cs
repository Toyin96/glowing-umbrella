using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Tests.Controllers
{
    public class LocationsControllerTests
    {
        private readonly Mock<IStateRetrieveService> _stateRetrieveServiceMock;
        private readonly LocationsController _controller;

        public LocationsControllerTests()
        {
            _stateRetrieveServiceMock = new Mock<IStateRetrieveService>();
            _controller = new LocationsController(_stateRetrieveServiceMock.Object);
        }

        [Fact]
        public async Task GetStatesAsync_ReturnsOk()
        {
            // Arrange
            var states = new List<StateResponse>(); // Populate with test data
            _stateRetrieveServiceMock.Setup(x => x.GetStatesAsync())
                .ReturnsAsync(new ListResponse<StateResponse>("Operation was successful") { Data = states });

            // Act
            var result = await _controller.GetStatesAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseData = Assert.IsType<ListResponse<StateResponse>>(okResult.Value);
            Assert.Equal(states, responseData.Data);
        }

        [Fact]
        public async Task GetRegionsAsync_ReturnsOk()
        {
            // Arrange
            var regions = new List<RegionResponse>(); // Populate with test data
            _stateRetrieveServiceMock.Setup(x => x.GetRegionsAsync())
                .ReturnsAsync(new ListResponse<RegionResponse> ("Operation was successful") { Data = regions });

            // Act
            var result = await _controller.GetRegionsAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseData = Assert.IsType<ListResponse<RegionResponse>>(okResult.Value);
            Assert.Equal(regions, responseData.Data);
        }

        [Fact]
        public async Task GetStatesUnderRegionAsync_ValidRegionId_ReturnsOk()
        {
            // Arrange
            var regionId = Guid.NewGuid();

            // Sample state data
            var state1 = new StateResponse { Id = Guid.NewGuid(), Name = "Lagos" };
            var state2 = new StateResponse { Id = Guid.NewGuid(), Name = "Abuja" };
            var state3 = new StateResponse { Id = Guid.NewGuid(), Name = "Kano" };

            // Populate the states list
            var states = new List<StateResponse> { state1, state2, state3 };

            _stateRetrieveServiceMock.Setup(x => x.GetStatesUnderRegionAsync(regionId))
                .ReturnsAsync(new ListResponse<StateResponse>("Operation was successful") { Data = states, Total = states.Count });

            // Act
            var result = await _controller.GetStatesUnderRegionAsync(regionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseData = Assert.IsType<ListResponse<StateResponse>>(okResult.Value);
            Assert.Equal(states, responseData.Data);
            Assert.Equal(3, responseData.Total);
        }

        [Fact]
        public async Task GetStatesUnderRegionAsync_InvalidRegionId_ReturnsNotFound()
        {
            // Arrange
            var invalidRegionId = Guid.NewGuid();
            _stateRetrieveServiceMock.Setup(x => x.GetStatesUnderRegionAsync(invalidRegionId))
                .ReturnsAsync(new ListResponse<StateResponse>("Operation was successful") { Data = null });

            // Act
            var result = await _controller.GetStatesUnderRegionAsync(invalidRegionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var responseData = Assert.IsType<ListResponse<StateResponse>>(okResult.Value);
            Assert.Equal("00", responseData.Code);
            Assert.Equal(0, responseData.Total);
        }
    }
}
