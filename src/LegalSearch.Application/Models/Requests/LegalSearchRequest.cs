using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests
{
    public class LegalSearchRequest
    {
        [JsonIgnore]
        public string? StaffId { get; set; }
        public required string RequestType { get; set; }
        public Guid BusinessLocation { get; set; }
        public Guid RegistrationLocation { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string CustomerAccountNumber { get; set; }
        public required string RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? AdditionalInformation { get; set; }
        public required List<IFormFile> RegistrationDocuments { get; set; }
    }
}
