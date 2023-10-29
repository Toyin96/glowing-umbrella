namespace LegalSearch.Application.Models.Requests.CSO
{
    public class CancelRequest
    {
        public required Guid RequestId { get; set; }
        public required string Reason { get; set; }
    }
}
