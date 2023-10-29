using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Infrastructure.Services.User
{
    public class ZonalManagerService : IZonalManagerService
    {
        private readonly IZonalServiceManager _zonalServiceManager;

        public ZonalManagerService(IZonalServiceManager zonalServiceManager)
        {
            _zonalServiceManager = zonalServiceManager;
        }

        public async Task<ListResponse<ZonalServiceManagerMiniDto>> GetZonalServiceManagers()
        {
            var zonalServiceManagers = await _zonalServiceManager.GetAllZonalServiceManagersInfo();

            return new ListResponse<ZonalServiceManagerMiniDto>("Operation was success", ResponseCodes.Success)
            {
                Data = zonalServiceManagers,
                Total = zonalServiceManagers.Count()
            };
        }
    }
}
