using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class ReturnRequest
    {
        public Guid RequestId { get; set; }
        public string? Feedback { get; set; }
        public List<IFormFile> SupportingDocuments { get; set; }
    }
}
