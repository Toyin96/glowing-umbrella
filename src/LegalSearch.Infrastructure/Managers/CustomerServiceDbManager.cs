using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    internal class CustomerServiceDbManager : ICustomerServiceManager
    {
        private readonly AppDbContext _appDbContext;

        public CustomerServiceDbManager(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<IEnumerable<CustomerServiceManagerMiniDto>> GetCustomerServiceManagers()
        {
            var managers = await _appDbContext.CustomerServiceManagers.Select(x => new CustomerServiceManagerMiniDto
            {
                SolId = x.SolId,
                Name = x.Name,
                EmailAddress = x.EmailAddress,
                AlternateEmailAddress = x.AlternateEmailAddress,
            }).ToListAsync();

            return managers ?? Enumerable.Empty<CustomerServiceManagerMiniDto>();
        }
    }
}
