using Fcmb.Shared.Models.Requests;
using LegalSearch.Domain.Enums.LegalRequest;

namespace LegalSearch.Application.Models.Requests
{
    public record ViewRequestAnalyticsPayload : PaginatedDateRangeRequest
    {
        public RequestStatusType? RequestStatus { get; set; }
    }
}
