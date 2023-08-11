using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class AcceptRequest
    {
        public Guid RequestId { get; set; }
        [JsonIgnore]
        public Guid SolicitorId { get; set; }
    }
}
