using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Location
{
    public interface IStateRetrieveService
    {
        Task<ListResponse<StateResponse>> GetStatesAsync();
        Task<ListResponse<LgaResponse>> GetRegionsAsync(Guid stateId);
    }
}
