using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalSearch.Test.Services
{
    public class StateRetrieveServiceTests
    {
        [Fact]
        public async Task GetRegionOfState_Should_ReturnRegionId_When_StateExists()
        {
            // Create options for the in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryAppDatabase")
                .Options;

            var stateId = Guid.NewGuid();
            var regionId = Guid.NewGuid();
            var region = new Region() { Name = "South West", Id = regionId };

            // Seed data into the in-memory database
            using (var context = new AppDbContext(options))
            {
                // Add test data
                context.States.Add(new State { Id = stateId, RegionId = regionId, Name = "Lagos", Region =  region});
                context.SaveChanges();
            }

            // Run the test using the in-memory database
            using (var context = new AppDbContext(options))
            {
                var service = new StateRetrieveService(context, Mock.Of<ILogger<StateRetrieveService>>());
                var result = await service.GetRegionOfState(stateId);
                Assert.Equal(regionId, result);
            }
        }

        [Fact]
        public async Task GetStatesAsync_Should_ReturnListOfStates()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryAppDatabase1")
                .Options;

            var regionId = Guid.NewGuid();
            var region = new Region() { Name = "South West", Id = regionId };

            using (var context = new AppDbContext(options))
            {
                context.States.Add(new State { Id = Guid.NewGuid(), Name = "State 1", Region = region });
                context.States.Add(new State { Id = Guid.NewGuid(), Name = "State 2", Region = region });
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var service = new StateRetrieveService(context, Mock.Of<ILogger<StateRetrieveService>>());
                var result = await service.GetStatesAsync();
                Assert.Equal(2, result.Data.Count());
            }
        }

        [Fact]
        public async Task GetStatesUnderRegionAsync_Should_ReturnListOfStatesUnderRegion()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryAppDatabase")
                .Options;

            var regionId = Guid.NewGuid();
            var region = new Region() { Name = "South West", Id = regionId };

            using (var context = new AppDbContext(options))
            {
                context.States.Add(new State { Id = Guid.NewGuid(), Name = "State 1", Region = region });
                context.States.Add(new State { Id = Guid.NewGuid(), Name = "State 2", Region = region });
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var service = new StateRetrieveService(context, Mock.Of<ILogger<StateRetrieveService>>());
                var result = await service.GetStatesUnderRegionAsync(regionId);
                Assert.Equal(2, result.Data.Count());
            }
        }
    }
}
