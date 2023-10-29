using LegalSearch.Domain.Entities.Location;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Location;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Services
{
    public class BranchRetrieveServiceTests
    {
        [Fact]
        public async Task GetBranchBySolId_WithValidId_ReturnsBranch()
        {
            // Arrange
            var solId = "123"; // Replace with a valid SolId
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase1")
                .Options;

            var dbContext = new AppDbContext(dbContextOptions);

            // Add sample branch data to the in-memory database
            dbContext.Branches.Add(new Branch { SolId = "123", Address = "Sample Branch Address" });
            dbContext.SaveChanges();

            var branchRetrieveService = new BranchRetrieveService(dbContext);

            // Act
            var result = await branchRetrieveService.GetBranchBySolId(solId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(solId, result.SolId);
        }

        [Fact]
        public async Task GetBranchBySolId_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var solId = "456"; // Replace with an invalid SolId
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;

            var dbContext = new AppDbContext(dbContextOptions);

            // Add sample branch data to the in-memory database
            dbContext.Branches.Add(new Branch { SolId = "123", Address = "Sample Branch Address" });
            dbContext.SaveChanges();

            var branchRetrieveService = new BranchRetrieveService(dbContext);

            // Act
            var result = await branchRetrieveService.GetBranchBySolId(solId);

            // Assert
            Assert.Null(result);
        }
    }
}
