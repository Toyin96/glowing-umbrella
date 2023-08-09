using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests
{
    public class LegalSearchRequest
    {
        [JsonIgnore]
        public string StaffId { get; set; }
        public string RequestType { get; set; }
        public Guid BusinessLocation { get; set; }
        public Guid RegistrationLocation { get; set; }
        public string CustomerAccount { get; set; }
        public string RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string AdditionalInformation { get; set; }
        public List<IFormFile> SupportingDocuments { get; set; }
    }
}
