using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LegalSearch.Test.Managers
{
    public class CustomerServiceDbManagerTests
    {
        [Fact]
        public async Task GetCustomerServiceManagers_ShouldReturnManagers()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using (var context = new AppDbContext(dbContextOptions))
            {
                context.CustomerServiceManagers.Add(new CustomerServiceManager
                {
                    SolId = "1",
                    Name = "Manager 1",
                    EmailAddress = "manager1@example.com",
                    AlternateEmailAddress = "altmanager1@example.com",
                });

                context.CustomerServiceManagers.Add(new CustomerServiceManager
                {
                    SolId = "2",
                    Name = "Manager 2",
                    EmailAddress = "manager2@example.com",
                    AlternateEmailAddress = "altmanager2@example.com",
                });

                context.SaveChanges();
            }

            using (var context = new AppDbContext(dbContextOptions))
            {
                var customerServiceDbManager = new CustomerServiceDbManager(context);

                // Act
                var managers = await customerServiceDbManager.GetCustomerServiceManagers();

                // Assert
                Assert.NotNull(managers);
                Assert.Equal(2, managers.Count());
            }
        }
    }
}
