using LegalSearch.Application.Models.Responses.CSO;
using LegalSearch.Domain.Entities.LegalRequest;
using Microsoft.AspNetCore.Http;

namespace LegalSearch.Application.Models.Responses
{
    public class LegalSearchResponsePayload
    {
        public Guid Id { get; set; }
        public required string RequestInitiator { get; set; }
        public required string RequestType { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string RequestStatus { get; set; }
        public required string CustomerAccountNumber { get; set; }
        public required string BusinessLocation { get; set; }
        public required Guid BusinessLocationId { get; set; }
        public required string RegistrationLocation { get; set; }
        public DateTime? RequestSubmissionDate { get; set; }
        public required Guid RegistrationLocationId { get; set; }
        public required string RegistrationNumber { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateDue { get; set; }
        public string Solicitor { get; set; }
        public required string ReasonOfCancellation { get; set; }
        public DateTime? DateOfCancellation { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Region { get; set; }
        public Guid RegionCode { get; set; }
        public ICollection<DiscussionDto> Discussions { get; set; } = new List<DiscussionDto>();
        public ICollection<RegistrationDocumentDto> RegistrationDocuments { get; set; } = new List<RegistrationDocumentDto>();
        public ICollection<RegistrationDocumentDto> SupportingDocuments { get; set; } = new List<RegistrationDocumentDto>();
    }

    public class LegalSearchRootResponsePayload
    {
        public required List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public List<MonthlyRequestData> RequestsByMonth { get; set; }
        public int TotalRequestsCount { get; set; }
        public int AssignedRequestsCount { get; set; }
        public int CompletedRequestsCount { get; set; }
        public int NewRequestsCount { get; set; }
        public int ReturnedRequestsCount { get; set; }
        public int RejectedRequestsCount { get; set; }
        public int WithinSLACount { get; set; }
        public int ElapsedSLACount { get; set; }
        public int Within3HoursToDueCount { get; set; }
    }

    public class MonthlyRequestData
    {
        public string Name { get; set; }
        public int New { get; set; }
        public int Comp { get; set; }
    }
}
