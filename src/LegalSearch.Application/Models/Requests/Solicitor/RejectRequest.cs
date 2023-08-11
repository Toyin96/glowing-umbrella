using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class RejectRequest
    {
        public Guid RequestId { get; set; }
        [JsonIgnore]
        public Guid SolicitorId { get; set; }
        public string? RejectionMessage { get; set; }
    }
}
