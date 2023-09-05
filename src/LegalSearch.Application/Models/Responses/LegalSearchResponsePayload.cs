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
        public required string RegistrationLocation { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateDue { get; set; }
        public DateTime RegistrationDate { get; set; }
        public ICollection<DiscussionDto> Discussions { get; set; } = new List<DiscussionDto>();
        public ICollection<RegistrationDocumentDto> RegistrationDocuments { get; set; } = new List<RegistrationDocumentDto>();
        public ICollection<RegistrationDocumentDto> SupportingDocuments { get; set; } = new List<RegistrationDocumentDto>();
    }

    public class LegalSearchRootResponsePayload
    {
        public required List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public int TotalRequests { get; set; }
        public int WithinSLACount { get; set; }
        public int ElapsedSLACount { get; set; }
        public int Within3HoursToDueCount { get; set; }
    }
}
