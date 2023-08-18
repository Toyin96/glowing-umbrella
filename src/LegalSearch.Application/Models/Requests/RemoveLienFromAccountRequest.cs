namespace LegalSearch.Application.Models.Requests
{
    public class RemoveLienFromAccountRequest
    {
        public string RequestID { get; set; }
        public string AccountNo { get; set; }
        public decimal AmountValue { get; set; }
        public string CurrencyCode { get; set; }
        public string Rmks { get; set; }
        public string ReasonCode { get; set; }
    }
}
