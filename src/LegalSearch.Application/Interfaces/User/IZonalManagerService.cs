using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Interfaces.User
{
    public interface IZonalManagerService
    {
        Task<ListResponse<ZonalServiceManagerMiniDto>> GetZonalServiceManagers();
    }
}
