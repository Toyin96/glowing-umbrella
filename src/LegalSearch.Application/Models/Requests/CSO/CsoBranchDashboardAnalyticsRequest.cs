using Fcmb.Shared.Models.Requests;
using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests.CSO
{
    public record CsoBranchDashboardAnalyticsRequest : PaginatedDateRangeRequest
    {
        [JsonIgnore]
        public string? BranchId { get; set; }
    }
}
