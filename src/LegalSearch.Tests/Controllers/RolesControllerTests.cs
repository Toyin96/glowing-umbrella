using Fcmb.Shared.Models.Responses;
using LegalSearch.Api.Controllers;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Requests;
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
    public class RolesControllerTests
    {
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly RolesController _controller;

        public RolesControllerTests()
        {
            _roleServiceMock = new Mock<IRoleService>();
            _controller = new RolesController(_roleServiceMock.Object);
        }

        [Fact]
        public async Task AddRole_ValidRequest_ReturnsOk()
        {
            // Arrange
            var roleRequest = new RoleRequest
            {
                RoleName = "TestRole",
                Permissions = new List<string> { "Permission1", "Permission2" }
            };

            var roleResponse = new RoleResponse
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleRequest.RoleName,
                Permissions = roleRequest.Permissions
            };

            _roleServiceMock.Setup(x => x.CreateRoleAsync(roleRequest))
                            .ReturnsAsync(new ObjectResponse<RoleResponse>("Operation was successful") { Data = roleResponse });

            // Act
            var result = await _controller.AddRole(roleRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ObjectResponse<RoleResponse>>(okResult.Value);
            Assert.Equal(roleResponse, response.Data);
        }

        [Fact]
        public async Task GetRoles_ValidRequest_ReturnsOk()
        {
            // Arrange
            var filterRoleRequest = new FilterRoleRequest
            {
                RoleName = "TestRole",
                Permissions = new List<string> { "Permission1", "Permission2" }
            };

            var roleResponses = new List<RoleResponse>
            {
                new RoleResponse
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = "TestRole1",
                    Permissions = new List<string> { "Permission1", "Permission2" }
                },
                new RoleResponse
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = "TestRole2",
                    Permissions = new List<string> { "Permission3", "Permission4" }
                }
            };

            _roleServiceMock.Setup(x => x.GetAllRolesAsync(filterRoleRequest))
                            .ReturnsAsync(new ListResponse<RoleResponse>("Operation was successful") { Data = roleResponses, Total = roleResponses.Count });

            // Act
            var result = await _controller.GetRoles(filterRoleRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ListResponse<RoleResponse>>(okResult.Value);
            Assert.Equal(roleResponses, response.Data);
            Assert.Equal(2, response.Total);
        }
    }
}
