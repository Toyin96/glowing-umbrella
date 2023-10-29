using Fcmb.Shared.Models.Requests;
using LegalSearch.Domain.Enums.LegalRequest;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public record SolicitorRequestAnalyticsPayload : PaginatedDateRangeRequest
    {
        public SolicitorRequestStatusType? RequestStatus { get; set; }
        public required ReportFormatType ReportFormatType { get; set; }
    }
}
