using Fcmb.Shared.Models.Requests;
using LegalSearch.Domain.Enums.LegalRequest;

namespace LegalSearch.Application.Models.Requests.CSO
{
    public record CsoDashboardAnalyticsRequest : PaginatedDateRangeRequest
    {
        public CsoRequestStatusType? CsoRequestStatusType { get; set; }
        public string? BranchId { get; set; }
    }
}
