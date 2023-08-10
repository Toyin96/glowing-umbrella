using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;

namespace LegalSearch.Application.Interfaces.Location
{
    public interface IStateRetrieveService
    {
        Task<ListResponse<StateResponse>> GetStatesAsync();
        Task<State> GetStateById(Guid id);
        Task<Guid> GetRegionOfState(Guid stateId);
        Task<ListResponse<LgaResponse>> GetRegionsAsync(Guid stateId);
    }
}
