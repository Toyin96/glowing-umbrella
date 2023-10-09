namespace LegalSearch.Application.Models.Responses
{
    public class SolicitorRetrievalResponse
    {
        public required Guid SolicitorId { get; set; }
        public required string SolicitorEmail { get; set; }
    }
}
