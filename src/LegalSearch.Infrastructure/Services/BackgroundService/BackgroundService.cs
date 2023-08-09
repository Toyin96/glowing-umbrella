using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Services.BackgroundService
{
    internal class BackgroundService : IBackgroundService
    {
        private readonly AppDbContext _appDbContext;

        public BackgroundService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task AssignRequestToSolicitors(Guid requestId)
        {
            // Load the request and perform assignment logic
            var request = await _appDbContext.LegalSearchRequests.FindAsync(requestId);
            if (request == null) return;

            // Implement solicitor assignment logic here
            var solicitors = DetermineSolicitors(request);

            // Update the request status and assigned solicitor(s)
            request.Status = "Assigned";
            // ...

            await _appDbContext.SaveChangesAsync();

            // Notify solicitors and other parties
            // ...
        }

        private async Task<List<Solicitor>> DetermineSolicitors(LegalRequest request)
        {
            // Implement logic to determine solicitors based on request criteria
            List<Guid> firms = new List<Guid>();

            switch (request.RequestType)
            {
                case nameof(RequestType.Corporate):
                    firms = await _appDbContext.Firms.Where(x => x.StateId == request.BusinessLocation)
                                                     .Select(x => x.Id)
                                                     .ToListAsync();
                    break;
                case nameof(RequestType.BusinessName):
                    if (request.BusinessLocation == request.RegistrationLocation)
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateId == request.BusinessLocation)
                                                         .Select(x => x.Id)
                                                         .ToListAsync();
                    }
                    else
                    {
                        firms = await _appDbContext.Firms.Where(x => x.StateId == request.RegistrationLocation)
                                                         .Select(x => x.Id)
                                                         .ToListAsync();
                    }
                    break;
                default:
                    break;
            }

            if (firms.Count == 0)
            {
                // route to Legal perfection team
            }

            // get solicitors
            List<string?> solicitorEmails = await _appDbContext.Users.Include(x => x.Firm)
                                                                     .Where(x => firms.Contains(x.Firm.Id))
                                                                     .Select(x => x.Email)
                                                                     .ToListAsync();



            throw new NotImplementedException();
        }

        private async Task SendAssignmentNotification(Solicitor solicitor, LegalRequest request)
        {
            // Implement logic to send assignment notifications to solicitors

            throw new NotImplementedException();
        }

        private async Task NotifyLegalPerfectionTeam(LegalRequest request)
        {
            // Implement notification logic here
            // This could involve sending an email, creating a task, etc.
            // Use your notification service or task management system
        }
    }
}
