using LegalSearch.Application.Models.Requests.Solicitor;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.User;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Test.Infrastructure.Managers
{
    public class SolicitorManagerTests
    {
        [Fact]
        public async Task DetermineSolicitors_ReturnsSolicitors()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var location = Guid.NewGuid();
                var firmId = Guid.NewGuid();
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample firms and users to the in-memory database
                var firm = new Firm { Id = firmId, Name = "Sample firm", Address = "sample location", StateOfCoverageId = location };
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "test",
                    StateId = location,
                    FirmId = location,
                    Firm = firm,
                    ProfileStatus = nameof(ProfileStatusType.Active),
                    OnboardingStatus = OnboardingStatusType.Completed
                };

                dbContext.Firms.Add(firm);
                dbContext.Users.Add(user);
                dbContext.SaveChanges();

                var legalRequest = new LegalRequest
                {
                    RequestType = nameof(RequestType.Corporate),
                    BusinessLocation = location,
                    RegistrationLocation = location,
                    BranchId = "061",
                    Status = nameof(RequestStatusType.LawyerAccepted),
                    CustomerAccountName = "Test user",
                    CustomerAccountNumber = "0123456789"
                };

                // Act
                var result = await solicitorManager.DetermineSolicitors(legalRequest);

                // Assert
                Assert.NotNull(result);
                Assert.Single(result);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public async Task EditSolicitorProfile_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add a sample user to the in-memory database
                Guid solicitorId = Guid.NewGuid();
                Guid regionId = Guid.NewGuid();
                Guid stateId = Guid.NewGuid();
                var firmId = Guid.NewGuid();

                var firm = new Firm { Id = firmId, Name = "Sample firm", Address = "sample location", StateOfCoverageId = stateId };
                State state = new State { Id = stateId, Name = "Lagos", Region = new Region { Name = "South west", Id = regionId } };

                var user = new User { Id = solicitorId, FirstName = "test", State = state, Firm = firm };

                dbContext.Users.Add(user);
                dbContext.SaveChanges();

                EditSolicitorProfileByLegalTeamRequest editRequest = new EditSolicitorProfileByLegalTeamRequest
                {
                    SolicitorId = solicitorId,
                    FirstName = "John",
                    LastName = "Doe",
                    FirmName = "Sample Firm",
                    Email = "johndoe@example.com",
                    PhoneNumber = "123-456-7890",
                    State = stateId,
                    Address = "123 Main St",
                    AccountNumber = "1234567890"
                };

                // Act
                var result = await solicitorManager.EditSolicitorProfile(editRequest, user.Id);

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public async Task FetchSolicitorsInSameRegion_ReturnsSolicitors()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase3")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample users to the in-memory database
                var location = Guid.NewGuid();
                var firmId = Guid.NewGuid();
                var regionId = Guid.NewGuid(); // Replace with the actual region ID
                State state = new State { Name = "Lagos", Region = new Region { Name = "South west", Id = regionId } };
                var firm = new Firm { Id = firmId, Name = "Sample firm", Address = "sample location", StateOfCoverageId = location, State = state };
                var user1 = new User { Id = Guid.NewGuid(), FirstName = "test user 1", Firm = firm, ProfileStatus = nameof(ProfileStatusType.Active), };
                var user2 = new User { Id = Guid.NewGuid(), FirstName = "test user 1", Firm = firm, ProfileStatus = nameof(ProfileStatusType.Active), };

                dbContext.Users.Add(user1);
                dbContext.Users.Add(user2);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorManager.FetchSolicitorsInSameRegion(regionId);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public async Task GetCurrentSolicitorMappedToRequest_ReturnsSolicitorAssignment()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase4")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample solicitor assignment records to the in-memory database
                var requestId = Guid.NewGuid(); // Replace with the actual request ID
                var solicitorId = Guid.NewGuid(); // Replace with the actual solicitor ID

                var assignment = new SolicitorAssignment { RequestId = requestId, SolicitorId = solicitorId, IsCurrentlyAssigned = true, SolicitorEmail = "sample@email.com" };

                dbContext.SolicitorAssignments.Add(assignment);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorManager.GetCurrentSolicitorMappedToRequest(requestId, solicitorId);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.IsCurrentlyAssigned);
            }
        }

        [Fact]
        public async Task GetNextSolicitorInLine_ReturnsSolicitorAssignment()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase5")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample solicitor assignment records to the in-memory database
                var requestId = Guid.NewGuid();

                var assignment1 = new SolicitorAssignment { RequestId = requestId, IsAccepted = false, AssignedAt = DateTime.UtcNow.AddHours(-1), Order = 1, SolicitorEmail = "sample@email.com" };
                var assignment2 = new SolicitorAssignment { RequestId = requestId, IsAccepted = false, AssignedAt = DateTime.UtcNow.AddHours(-2), Order = 2, SolicitorEmail = "sample@email.com" };

                dbContext.SolicitorAssignments.Add(assignment1);
                dbContext.SolicitorAssignments.Add(assignment2);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorManager.GetNextSolicitorInLine(requestId, assignment1.Order);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Order);
            }
        }

        [Fact]
        public async Task GetRequestsToReroute_ReturnsRequestIds()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase6")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample solicitor assignment records to the in-memory database
                var requestId1 = Guid.NewGuid(); // Replace with the actual request ID
                var requestId2 = Guid.NewGuid(); // Replace with the actual request ID

                var assignment1 = new SolicitorAssignment { RequestId = requestId1, IsAccepted = true, IsCurrentlyAssigned = true, AssignedAt = DateTime.UtcNow.AddHours(-73), SolicitorEmail = "sample@email.com" };
                var assignment2 = new SolicitorAssignment { RequestId = requestId2, IsAccepted = true, IsCurrentlyAssigned = true, AssignedAt = DateTime.UtcNow.AddHours(-25), SolicitorEmail = "sample@email.com" };

                dbContext.SolicitorAssignments.Add(assignment1);
                dbContext.SolicitorAssignments.Add(assignment2);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorManager.GetRequestsToReroute();

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public async Task GetUnattendedAcceptedRequestsForTheTimeFrame_ReturnsRequestIds()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase7")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample solicitor assignment records to the in-memory database
                var requestId1 = Guid.NewGuid(); // Replace with the actual request ID
                var requestId2 = Guid.NewGuid(); // Replace with the actual request ID

                var assignment1 = new SolicitorAssignment { RequestId = requestId1, IsAccepted = true, IsCurrentlyAssigned = true, AssignedAt = DateTime.UtcNow.AddHours(-25), SolicitorEmail = "sample@email.com" };
                var assignment2 = new SolicitorAssignment { RequestId = requestId2, IsAccepted = true, IsCurrentlyAssigned = true, AssignedAt = DateTime.UtcNow.AddHours(-74), SolicitorEmail = "sample@email.com" };

                dbContext.SolicitorAssignments.Add(assignment1);
                dbContext.SolicitorAssignments.Add(assignment2);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorManager.GetUnattendedAcceptedRequestsForTheTimeFrame(DateTime.UtcNow.AddHours(-24), isSlaElapsed: false);

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);
            }
        }

        [Fact]
        public async Task UpdateManySolicitorAssignmentStatuses_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase8")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var logger = new Mock<ILogger<SolicitorManager>>();
                var solicitorManager = new SolicitorManager(dbContext, logger.Object);

                // Add sample solicitor assignment records to the in-memory database
                var assignment1 = new SolicitorAssignment { Id = Guid.NewGuid(), IsCurrentlyAssigned = true, SolicitorEmail = "sample@email.com" };
                var assignment2 = new SolicitorAssignment { Id = Guid.NewGuid(), IsCurrentlyAssigned = true, SolicitorEmail = "sample@email.com" };

                dbContext.SolicitorAssignments.Add(assignment1);
                dbContext.SolicitorAssignments.Add(assignment2);
                dbContext.SaveChanges();

                var solicitorAssignmentIds = new List<Guid> { assignment1.Id, assignment2.Id };

                // Act
                var result = await solicitorManager.UpdateManySolicitorAssignmentStatuses(solicitorAssignmentIds);

                // Assert
                Assert.True(result);
                Assert.False(assignment1.IsCurrentlyAssigned);
                Assert.False(assignment2.IsCurrentlyAssigned);
            }
        }
    }

}
