using Microsoft.AspNetCore.Http;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class ReturnRequest
    {
        public Guid RequestId { get; set; }
        public string? Feedback { get; set; }
        public List<IFormFile> SupportingDocuments { get; set; } = new List<IFormFile>();
    }
}
