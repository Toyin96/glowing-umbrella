using Microsoft.AspNetCore.Http;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class SubmitLegalSearchReport
    {
        public Guid RequestId { get; set; }
        public string? Feedback { get; set; }
        public List<IFormFile> RegistrationDocuments { get; set; } = new List<IFormFile>();
    }
}
