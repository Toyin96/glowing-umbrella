namespace LegalSearch.Application.Models.Responses
{
    public class SolicitorRetrievalResponse
    {
        public Guid SolicitorId { get; set; }
        public required string SolicitorEmail { get; set; }
    }
}
