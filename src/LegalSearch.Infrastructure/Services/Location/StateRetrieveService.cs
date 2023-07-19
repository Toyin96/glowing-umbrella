using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.Location
{
    public class StateRetrieveService : IStateRetrieveService
    {
        private readonly ISessionService _sessionService;
        private readonly AppDbContext _appDbContext;
        private readonly ILogger<StateRetrieveService> _logger;

        public StateRetrieveService(ISessionService sessionService, 
            AppDbContext appDbContext, ILogger<StateRetrieveService> logger)
        {
            _sessionService = sessionService;
            _appDbContext = appDbContext;
            _logger = logger;
        }

        public async Task<ListResponse<LgaResponse>> GetRegionsAsync(Guid stateId)
        {
            var session = _sessionService.GetUserSession();

            if (session is null) return new ListResponse<LgaResponse>("User Is Unauthenticated", ResponseCodes.Unauthenticated);

            var lgas = await _appDbContext.Lgas.Where(x => x.StateId == stateId).ToListAsync();

            if (lgas is null)
            {
                _logger.LogInformation("Regions with state ID {ID} not found", stateId);

                return new ListResponse<LgaResponse>("Regions Not Found", ResponseCodes.DataNotFound);
            }

            return new ListResponse<LgaResponse>("Successfully Retrieved regions")
            {
                Data = lgas.Select(x => new LgaResponse { Name = x.Name}).ToList(),
                Total = lgas.Count
            };
        }

        public async Task<ListResponse<StateResponse>> GetStatesAsync()
        {
            var session = _sessionService.GetUserSession();

            if (session is null) return new ListResponse<StateResponse>("User Is Unauthenticated", ResponseCodes.Unauthenticated);

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
