using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Managers
{
    internal class ZonalServiceDbManager : IZonalServiceManager
    {
        private readonly AppDbContext _context;

        public ZonalServiceDbManager(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> AddZonalServiceManager(ZonalServiceManager zonalServiceManager)
        {
            _context.ZonalServiceManagers.Add(zonalServiceManager);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ZonalServiceManager>> GetAllZonalServiceManagers()
        {
            return await _context.ZonalServiceManagers.ToListAsync();
        }

        public async Task<IEnumerable<ZonalServiceManagerMiniDto>> GetAllZonalServiceManagersInfo()
        {
            var managers = await _context.ZonalServiceManagers.Select(x => new ZonalServiceManagerMiniDto
            {
                Id = x.Id,
                Name = x.Name,
                EmailAddress = x.EmailAddress
            }).ToListAsync();

            return managers ?? Enumerable.Empty<ZonalServiceManagerMiniDto>();
        }
    }
}
