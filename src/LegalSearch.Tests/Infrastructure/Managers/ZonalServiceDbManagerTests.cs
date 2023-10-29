using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Test.Infrastructure.Managers
{
    public class ZonalServiceDbManagerTests
    {
        [Fact]
        public async Task AddZonalServiceManager_ReturnsTrueOnSuccess()
        {
            // Arrange
            var zonalServiceManager = new ZonalServiceManager
            {
                Name = "sample zsm",
                EmailAddress = "sample@yahoo.com",
                AlternateEmailAddress = "sample2@yahoo.com",
            };
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new ZonalServiceDbManager(dbContext);

                // Act
                var result = await manager.AddZonalServiceManager(zonalServiceManager);

                // Assert
                Assert.True(result);
            }
        }

        [Fact]
        public async Task GetAllZonalServiceManagers_ReturnsAllManagers()
        {
            // Arrange
            var managers = new List<ZonalServiceManager>
            {
                new ZonalServiceManager
                {
                    Name = "sample zsm",
                    EmailAddress = "sample1@yahoo.com",
                    AlternateEmailAddress = "sample1@yahoo.com",
                },
                new ZonalServiceManager
                {
                    Name = "sample zsm 2",
                    EmailAddress = "sample2@yahoo.com",
                    AlternateEmailAddress = "sample2@yahoo.com",
                }
            };

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase2")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.ZonalServiceManagers.AddRange(managers);
                dbContext.SaveChanges();

                var manager = new ZonalServiceDbManager(dbContext);

                // Act
                var result = await manager.GetAllZonalServiceManagers();

                // Assert
                Assert.Equal(managers.Count, result.Count());
            }
        }

        [Fact]
        public async Task GetAllZonalServiceManagersInfo_ReturnsManagerInfo()
        {
            // Arrange
            var managers = new List<ZonalServiceManager>
            {
                new ZonalServiceManager
                {
                    Name = "sample zsm",
                    EmailAddress = "sample1@yahoo.com",
                    AlternateEmailAddress = "sample1@yahoo.com",
                },
                new ZonalServiceManager
                {
                    Name = "sample zsm 2",
                    EmailAddress = "sample2@yahoo.com",
                    AlternateEmailAddress = "sample2@yahoo.com",
                }
            };

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase3")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                dbContext.ZonalServiceManagers.AddRange(managers);
                dbContext.SaveChanges();

                var manager = new ZonalServiceDbManager(dbContext);

                // Act
                var result = await manager.GetAllZonalServiceManagersInfo();

                // Assert
                Assert.Equal(managers.Count, result.Count());
            }
        }
    }
}
