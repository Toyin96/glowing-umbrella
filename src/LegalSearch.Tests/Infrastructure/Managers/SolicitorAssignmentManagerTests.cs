using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Managers
{
    public class SolicitorAssignmentManagerTests
    {
        [Fact]
        public async Task GetSolicitorAssignmentBySolicitorId_ReturnsSolicitorAssignment()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase6")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var solicitorAssignmentManager = new SolicitorAssignmentManager(dbContext);

                // Add a sample SolicitorAssignment to the in-memory database
                var solicitorId = Guid.NewGuid();
                var requestId = Guid.NewGuid();
                var sampleAssignment = new SolicitorAssignment
                {
                    SolicitorId = solicitorId,
                    RequestId = requestId,
                    SolicitorEmail = "test!example.com"
                };
                dbContext.SolicitorAssignments.Add(sampleAssignment);
                dbContext.SaveChanges();

                // Act
                var result = await solicitorAssignmentManager.GetSolicitorAssignmentBySolicitorId(solicitorId, requestId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(solicitorId, result.SolicitorId);
                Assert.Equal(requestId, result.RequestId);
            }
        }

        [Fact]
        public async Task UpdateSolicitorAssignmentRecord_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase7")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var solicitorAssignmentManager = new SolicitorAssignmentManager(dbContext);

                // Add a sample SolicitorAssignment to the in-memory database
                var sampleAssignment = new SolicitorAssignment
                {
                    SolicitorId = Guid.NewGuid(),
                    RequestId = Guid.NewGuid(),
                    SolicitorEmail = "test!example.com"
                };
                dbContext.SolicitorAssignments.Add(sampleAssignment);
                dbContext.SaveChanges();

                // Modify the properties of the sampleAssignment
                sampleAssignment.SolicitorEmail = "test2example.com";

                // Act
                var result = await solicitorAssignmentManager.UpdateSolicitorAssignmentRecord(sampleAssignment);

                // Assert
                Assert.True(result);

                // Retrieve the updated assignment from the context
                var updatedAssignment = dbContext.SolicitorAssignments.FirstOrDefault(a => a.SolicitorId == sampleAssignment.SolicitorId && a.RequestId == sampleAssignment.RequestId);
                Assert.NotNull(updatedAssignment);
                Assert.Equal("test2example.com", updatedAssignment.SolicitorEmail);
            }
        }
    }

}
