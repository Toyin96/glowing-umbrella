namespace LegalSearch.Application.Models.Requests
{
    public class RemoveLienFromAccountRequest
    {
        public required string RequestID { get; set; }
        public required string AccountNo { get; set; }
        public required string LienId { get; set; }
        public required string CurrencyCode { get; set; }
        public required string Rmks { get; set; }
        public required string ReasonCode { get; set; }
    }
}
