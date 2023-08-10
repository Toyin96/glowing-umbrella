using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.Location
{
    public class StateRetrieveService : IStateRetrieveService
    {
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<StateRetrieveService> _logger;

        public StateRetrieveService(AppDbContext appDbContext, ILogger<StateRetrieveService> logger)
        {
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task<Guid> GetRegionOfState(Guid stateId)
        {
            var state = await _appDbContext.States.FirstOrDefaultAsync(x => x.Id == stateId);

            if (state == null)
            {
                return Guid.Empty;
            }

            return state.RegionId;
        }

        public async Task<ListResponse<LgaResponse>> GetRegionsAsync(Guid stateId)
        {
            var lgas = ""; // await _appDbContext.Regions.Where(x => x.StateId == stateId).ToListAsync();

            if (lgas is null)
            {
                _logger.LogInformation("Regions with state ID {ID} not found", stateId);

                return new ListResponse<LgaResponse>("Regions Not Found", ResponseCodes.DataNotFound);
            }

            return new ListResponse<LgaResponse>("Successfully Retrieved regions")
            {
                Data = null, //lgas.Select(x => new LgaResponse { Name = x.Name}).ToList(),
                Total = 1 //lgas.Count
            };
        }

        public async Task<State> GetStateById(Guid id)
        {
            return await _appDbContext.States.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ListResponse<StateResponse>> GetStatesAsync()
        {
            var response = await GetStatesQuery();

            return new ListResponse<StateResponse>("Successfully Retrieved states")
            {
                Data = response,
                Total = response.Count
            };
        }

        private async Task<List<StateResponse>> GetStatesQuery()
        {
            return await _appDbContext.States
                                       .AsNoTracking()
                                       .Select(x => new StateResponse
                                       {
                                           Name = x.Name,
                                       })
                                       .ToListAsync();
        }
    }
}
