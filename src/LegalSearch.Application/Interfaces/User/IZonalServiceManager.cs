using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Domain.Entities.User;

namespace LegalSearch.Application.Interfaces.User
{
    public interface IZonalServiceManager
    {
        Task<bool> AddZonalServiceManager(ZonalServiceManager zonalServiceManager);
        Task<IEnumerable<ZonalServiceManager>> GetAllZonalServiceManagers();
        Task<IEnumerable<ZonalServiceManagerMiniDto>> GetAllZonalServiceManagersInfo();
    }
}
