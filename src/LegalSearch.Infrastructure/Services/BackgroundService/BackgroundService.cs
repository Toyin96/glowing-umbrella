﻿using LegalSearch.Application.Interfaces.BackgroundService;
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
using LegalSearch.Infrastructure.Utilities;
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
        public async Task AssignRequestToSolicitorsJob(Guid requestId)
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

        public async Task CheckAndRerouteRequestsJob()
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
                    continue;
                }

                // get the currently assigned solicitor, know his/her order and route it to the next order
                var currentlyAssignedSolicitor = await _solicitorRetrievalService.GetCurrentSolicitorMappedToRequest(request, 
                    legalSearchRequest.AssignedSolicitorId);

                await PushRequestToNextSolicitorInOrder(request, currentlyAssignedSolicitor.Order);
            }
        }

        public async Task PushRequestToNextSolicitorInOrder(Guid requestId, int currentAssignedSolicitorOrder = 0)
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

            // logged time request was assigned to solicitor
            nextSolicitor.AssignedAt = TimeUtils.GetCurrentLocalTime();

            // Update the request status and assigned solicitor(s)
            request.Status = nameof(RequestStatusType.Lawyer);
            request.AssignedSolicitorId = nextSolicitor.SolicitorId; // Assuming you have a property to track assigned solicitor

            // Send notification to the solicitor
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "New Request",
                NotificationType = NotificationType.AssignedToSolicitor,
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
                    AssignedAt = TimeUtils.GetCurrentLocalTime(),
                    IsAccepted = false
                };

                _appDbContext.SolicitorAssignments.Add(assignment);
            }

            await _appDbContext.SaveChangesAsync();
        }

        private async Task NotifyLegalPerfectionTeam(LegalRequest request)
        {
            // form notification request
            var notification = new Domain.Entities.Notification.Notification
            {
                Title = "UnAssigned Request",
                NotificationType = NotificationType.UnAssignedRequest,
                Message = ConstantMessage.UnAssignedRequestMessage,
                MetaData = JsonSerializer.Serialize(request)
            };

            // Notify LegalPerfectionTeam of new request was unassigned
            await _notificationService.SendNotificationToRole(nameof(RoleType.LegalPerfectionTeam), notification);

            // update legalsearch request status
            request.Status = nameof(RequestStatusType.UnAssigned);
            await _legalSearchRequestManager.UpdateLegalSearchRequest(request);
        }

        public async Task NotificationReminderForUnAttendedRequestsJob()
        {
            #region Send a reminder notification after 24hours that a request has been assigned

            // resolves time to 24 hours ago
            var requestsAcceptedTwentyFoursAgo = await _solicitorRetrievalService.GetUnattendedAcceptedRequestsForTheTimeFrame(TimeUtils.GetTwentyHoursElapsedTime());

            if (requestsAcceptedTwentyFoursAgo != null && requestsAcceptedTwentyFoursAgo.Any())
            {
                // get the associated legalRequests
                Dictionary<Guid, LegalRequest> solicitorRequestsDictionary = new Dictionary<Guid, LegalRequest>();

                foreach (var request in requestsAcceptedTwentyFoursAgo.ToList())
                {
                    var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(request);

                    // could not find legal search request
                    if (legalSearchRequest == null) continue;

                    solicitorRequestsDictionary.Add(legalSearchRequest.AssignedSolicitorId, legalSearchRequest);
                }

                // process notification to solicitor in parallel
                Parallel.ForEach(solicitorRequestsDictionary, async individualSolicitorRequestsDictionary =>
                {
                    // Send notification to the solicitor
                    var notification = new Domain.Entities.Notification.Notification
                    {
                        Title = "Reminder Notification on Pending Request",
                        NotificationType = NotificationType.AssignedToSolicitor,
                        Message = ConstantMessage.RequestPendingWithSolicitorMessage,
                        MetaData = JsonSerializer.Serialize(individualSolicitorRequestsDictionary.Value)
                    };

                    await _notificationService.SendNotificationToUser(individualSolicitorRequestsDictionary.Key, notification);
                });
            }
            #endregion

            #region Re-route request to another solicitor after request SLA have elapsed

            // get assigned requests that have been unattended for the 72 hours (3 days)
            var requestsWithElapsedSLA = await _solicitorRetrievalService.GetUnattendedAcceptedRequestsForTheTimeFrame(TimeUtils.GetSeventyTwoHoursElapsedTime());

            // check if any matching request was returned
            if (requestsWithElapsedSLA != null && requestsWithElapsedSLA.Any())
            {
                Dictionary<Guid, int> elapsedSLARequestsDictionary = new Dictionary<Guid, int>();

                foreach (var requestId in requestsWithElapsedSLA.ToList())
                {
                    // get the associated legal search
                    var legalSearchRequest = await _legalSearchRequestManager.GetLegalSearchRequest(requestId);

                    // could not find legal search request
                    if (legalSearchRequest == null) continue;

                    // get the currently assigned solicitor to know his/her order
                    var currentlyAssignedSolicitor = await _solicitorRetrievalService.GetCurrentSolicitorMappedToRequest(requestId,
                        legalSearchRequest.AssignedSolicitorId);

                    elapsedSLARequestsDictionary.Add(requestId, currentlyAssignedSolicitor.Order);
                }

                // process each request serially
                foreach (var request in elapsedSLARequestsDictionary)
                {
                    // pass the request to another solicitor in-line based on the current solicitor's order
                    await PushRequestToNextSolicitorInOrder(request.Key, request.Value);
                }
            }

            #endregion
        }
    }
}
