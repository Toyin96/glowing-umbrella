using Azure.Core;
using LegalSearch.Application.Interfaces.BackgroundService;
using LegalSearch.Application.Interfaces.LegalSearchRequest;
using LegalSearch.Application.Interfaces.Location;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.ApplicationMessages;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Domain.Enums.Notification;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.Persistence;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.BackgroundService
{
    internal class BackgroundService : IBackgroundService
    {
        private readonly AppDbContext _appDbContext;
        private readonly INotificationService _notificationService;
        private readonly ISolicitorRetrievalService _solicitorRetrievalService;
        private readonly IStateRetrieveService _stateRetrieveService;
        private readonly ILegalSearchRequestManager _legalSearchRequestManager;

        public BackgroundService(AppDbContext appDbContext,
            INotificationService notificationService,
            ISolicitorRetrievalService solicitorRetrievalService,
            IStateRetrieveService stateRetrieveService, 
            ILegalSearchRequestManager legalSearchRequestManager)
        {
            _appDbContext = appDbContext;
            _notificationService = notificationService;
            _solicitorRetrievalService = solicitorRetrievalService;
            _stateRetrieveService = stateRetrieveService;
            _legalSearchRequestManager = legalSearchRequestManager;
        }
        public async Task AssignRequestToSolicitors(Guid requestId)
        {
            // Load the request and perform assignment logic
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);
            if (request == null) return;

            // Solicitor assignment logic is done here
            var solicitors = await _solicitorRetrievalService.DetermineSolicitors(request);

            if (solicitors == null || solicitors?.ToList()?.Count == 0)
            {
                // reroute to other states in the same region
                // Fetch solicitors in other states within the same region
                var region = await _stateRetrieveService.GetRegionOfState(request.BusinessLocation);
                solicitors = await _solicitorRetrievalService.FetchSolicitorsInSameRegion(region);

                var solicitorsList = solicitors.ToList();

                if (solicitorsList.Count == 0)
                {
                    // Route to Legal Perfection Team
                    await NotifyLegalPerfectionTeam(request);
                    return;
                }

                // Assign order to new solicitors in the same region
                await AssignOrdersAsync(requestId, solicitorsList);

                // route request to first solicitor based on order arrangement
                await PushRequestToNextSolicitorInOrder(requestId);

                return; // end process
            }

            // Update the request status and assigned orders to available solicitor(s)
            await AssignOrdersAsync(requestId, solicitors.ToList());

            // route request to first solicitor based on order arrangement
            await PushRequestToNextSolicitorInOrder(requestId);
        }

        public async Task CheckAndRerouteRequests()
        {
            // Implement logic to query for requests assigned to lawyers for 20 minutes
            var requestsToReroute = await _solicitorRetrievalService.GetRequestsToReroute();

            foreach (var request in requestsToReroute)
            {
                // Get the legalRequest entity and check its status
                var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                if (legalSearchRequest.Status == nameof(NotificationType.UnAssignedRequest))
                {
                    /*
                     This request has been routed to legalPerfection team either due to:
                        1. No matching solicitor
                        2. Every single matching solicitor has been assigned to it but no one accepted it on time.
                     */

                    break;
                }

                // get the currently assigned solicitor, know his/her order and route it to the next order
                var currentlyAssignedSolicitor = await _solicitorRetrievalService.GetCurrentSolicitorMappedToRequest(request, 
                    legalSearchRequest.AssignedSolicitorId);

                await PushRequestToNextSolicitorInOrder(request, currentlyAssignedSolicitor.Order);
            }
        }

        private async Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0)
        {
            var request = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

            // Get the next solicitor in line
            var nextSolicitor = await _solicitorRetrievalService.GetNextSolicitorInLine(requestId, currentAssignedSolicitorOrder);

            if (nextSolicitor == null)
            {
                // Route to Legal Perfection Team
                await NotifyLegalPerfectionTeam(request);
                return;
            }

            // Update the request status and assigned solicitor(s)
            request.Status = nameof(RequestStatusType.Lawyer);
            request.AssignedSolicitorId = nextSolicitor.SolicitorId; // Assuming you have a property to track assigned solicitor

            // Send notification to the solicitor
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "New Request",
                NotificationType = NotificationType.RequestPendingWithSolicitor,
                RecipientUserId = nextSolicitor.SolicitorId.ToString(),
                Message = ConstantMessage.NewRequestAssignmentMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // Notify solicitor of new request
            await _notificationService.SendNotificationToUser(nextSolicitor.SolicitorId, notification);

            await _appDbContext.SaveChangesAsync();
        }

        private async Task AssignOrdersAsync(Guid requestId, List<SolicitorRetrievalResponse> solicitors)
        {
            if (solicitors.Count == 0)
            {
                // No solicitors to assign
                return;
            }

            // Shuffle the solicitors list to randomize the order
            var random = new Random();
            var shuffledSolicitors = solicitors.OrderBy(_ => random.Next()).ToList();

            for (int i = 0; i < shuffledSolicitors.Count; i++)
            {
                var solicitorId = shuffledSolicitors[i].SolicitorId;
                var assignment = new SolicitorAssignment
                {
                    SolicitorId = solicitorId,
                    RequestId = requestId,
                    Order = i + 1,
                    AssignedAt = DateTime.UtcNow.AddHours(1),
                    IsAccepted = false
                };

                _appDbContext.SolicitorAssignments.Add(assignment);
            }

            await _appDbContext.SaveChangesAsync();
        }

        private async Task NotifyLegalPerfectionTeam(LegalRequest request)
        {
            // Send notification to the solicitor
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "UnAssigned Request",
                NotificationType = NotificationType.UnAssignedRequest,
                Message = ConstantMessage.UnAssignedRequestMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // Notify solicitor of new request
            await _notificationService.SendNotificationToRole(nameof(RoleType.LegalPerfectionTeam), notification);

            // update legalsearch request status
            request.Status = nameof(RequestStatusType.UnAssigned);
            await _legalSearchRequestManager.UpdateLegalSearchRequest(request);
        }
    }
}
