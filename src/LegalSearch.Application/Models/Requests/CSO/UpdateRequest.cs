using Microsoft.AspNetCore.Http;

namespace LegalSearch.Application.Models.Requests.CSO
{
    public class UpdateRequest
    {
        public Guid RequestId { get; set; }
        public required string RequestType { get; set; }
        public Guid BusinessLocation { get; set; }
        public Guid RegistrationLocation { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string CustomerAccountNumber { get; set; }
        public required string RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? ReasonForRejection { get; set; }
        public string? ReasonForCancelling { get; set; }
        public string? AdditionalInformation { get; set; }
        public required List<IFormFile> RegistrationDocuments { get; set; }
        public required List<IFormFile> SupportingDocuments { get; set; }
    }
}
