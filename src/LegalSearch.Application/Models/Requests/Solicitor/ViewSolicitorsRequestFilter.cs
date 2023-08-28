using Fcmb.Shared.Models.Requests;
using LegalSearch.Domain.Enums.User;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public record ViewSolicitorsRequestFilter : PaginatedRequest
    {
        public Guid RegionId { get; set; }
        public Guid Chamber { get; set; }
        public ProfileStatusType Status { get; set; }
    }
}
