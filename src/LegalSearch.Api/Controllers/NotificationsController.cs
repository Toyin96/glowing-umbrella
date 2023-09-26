using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Application.Models.Requests.Notification;
using LegalSearch.Application.Models.Responses;
using LegalSearch.Domain.Enums;
using LegalSearch.Domain.Enums.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LegalSearch.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = $"{nameof(RoleType.LegalPerfectionTeam)}, {nameof(RoleType.Cso)}, {nameof(RoleType.Solicitor)}")]
    public class NotificationsController : BaseController
    {
        private readonly IInAppNotificationService _notificationService;

        public NotificationsController(IInAppNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Marks the notification as read.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [HttpPost("MarkNotificationAsRead")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> MarkNotificationAsRead([FromBody] UpdateNotificationRequest request)
        {
            var response = await _notificationService.MarkNotificationAsRead(request.NotificationId);
            return HandleResponse(response);
        }

        /// <summary>
        /// Gets the pending notifications for user.
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetPendingNotificationsForUser")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ListResponse<NotificationResponse>>> GetPendingNotificationsForUser()
        {
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            var response = await _notificationService.GetNotificationsForUser(userId!);
            return HandleResponse(response);
        }

        /// <summary>
        /// Marks all notification as read.
        /// </summary>
        /// <returns></returns>
        [HttpPost("MarkAllNotificationAsRead")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<StatusResponse>> MarkAllNotificationAsRead()
        {
            string? userId = User.Claims.FirstOrDefault(x => x.Type == nameof(ClaimType.UserId))?.Value;
            var response = await _notificationService.MarkAllNotificationsAsRead(userId!);
            return HandleResponse(response);
        }
    }
}
