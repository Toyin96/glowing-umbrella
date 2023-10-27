using LegalSearch.Application.Models.Requests.CSO;
using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Test.Infrastructure.Managers
{
    public class LegalSearchRequestManagerTests
    {
        [Fact]
        public async Task AddNewLegalSearchRequest_ShouldAddRequest()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using (var context = new AppDbContext(dbContextOptions))
            {
                var logger = new Mock<ILogger<LegalSearchRequestManager>>();
                var manager = new LegalSearchRequestManager(context, logger.Object);

                var legalRequest = new LegalRequest
                {
                    BranchId = "061",
                    Status = nameof(RequestStatusType.Initiated),
                    CustomerAccountName = "TestUser 1",
                    CustomerAccountNumber = "0123456789",
                };

                // Act
                var result = await manager.AddNewLegalSearchRequest(legalRequest);

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public async Task GetLegalRequestsForSolicitor_ShouldReturnLegalSearchRootResponsePayload()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using (var context = new AppDbContext(dbContextOptions))
            {
                var logger = new Mock<ILogger<LegalSearchRequestManager>>();
                var manager = new LegalSearchRequestManager(context, logger.Object);

                // Add test data to the in-memory database
                // ...

                var viewRequestAnalyticsPayload = new SolicitorRequestAnalyticsPayload
                {
                    ReportFormatType = ReportFormatType.Pdf
                };
                var solicitorId = Guid.NewGuid();

                // Act
                var response = await manager.GetLegalRequestsForSolicitor(viewRequestAnalyticsPayload, solicitorId);

                // Assert
                Assert.NotNull(response);
            }
        }

        [Fact]
        public async Task UpdateLegalSearchRequest_ReturnsTrueOnSuccessfulUpdate()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var legalRequest = new LegalRequest
            {
                BranchId = "061",
                Status = nameof(RequestStatusType.Initiated),
                CustomerAccountName = "TestUser 1",
                CustomerAccountNumber = "0123456789",
            };

            using (var dbContext = new AppDbContext(options))
            {

                await dbContext.LegalSearchRequests.AddAsync(legalRequest);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestManager(dbContext, null);

                // Act
                var result = await manager.UpdateLegalSearchRequest(legalRequest);

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public async Task GetLegalRequestsForStaff_ReturnsStaffRootResponsePayload()
        {
            // Arrange
            var request = new StaffDashboardAnalyticsRequest();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestManager(dbContext, null);

                // Initialize and seed the in-memory database with test data
                dbContext.LegalSearchRequests.Add(new LegalRequest
                {
                    BranchId = "061",
                    Status = nameof(RequestStatusType.Initiated),
                    CustomerAccountName = "TestUser 1",
                    CustomerAccountNumber = "0123456789",
                });

                dbContext.SaveChanges();
            }

            // Act
            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestManager(dbContext, null);

                var result = await manager.GetLegalRequestsForStaff(request);

                // Assert
                Assert.NotNull(result);
            }
        }
    }
}
