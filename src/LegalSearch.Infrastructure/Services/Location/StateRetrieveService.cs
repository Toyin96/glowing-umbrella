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

        public async Task<ListResponse<RegionResponse>> GetRegionsAsync()
        {
            var regions = await _appDbContext.Regions.ToListAsync();

            if (regions is null)
            {
                _logger.LogInformation("No region found");

                return new ListResponse<RegionResponse>("Regions Not Found", ResponseCodes.DataNotFound);
            }

            return new ListResponse<RegionResponse>("Successfully Retrieved regions")
            {
                Data = regions.Select(x => new RegionResponse { Name = x.Name, Id = x.Id}).ToList(),
                Total = regions.Count
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

        public async Task<ListResponse<StateResponse>> GetStatesUnderRegionAsync(Guid regionId)
        {
             var states = await _appDbContext.States
                               .AsNoTracking()
                               .Where(x => x.RegionId == regionId)
                               .Select(x => new StateResponse
                               {
                                   Id = x.Id,
                                   Name = x.Name,
                               })
                               .ToListAsync();

            if (states is null)
            {
                _logger.LogInformation("No region found");

                return new ListResponse<StateResponse>("No State Found For The Region Provided", ResponseCodes.DataNotFound);
            }

            return new ListResponse<StateResponse>("Successfully Fetched States under Region", ResponseCodes.Success)
            {
                Data = states,
                Total = states.Count
            };
        }

        private async Task<List<StateResponse>> GetStatesQuery()
        {
            return await _appDbContext.States
                                       .AsNoTracking()
                                       .Select(x => new StateResponse
                                       {
                                           Id = x.Id,
                                           Name = x.Name,
                                       })
                                       .ToListAsync();
        }
    }
}
