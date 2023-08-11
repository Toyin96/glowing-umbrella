namespace LegalSearch.Application.Models.Requests
{
    internal class AddLienToAccountRequest
    {
        public string RequestID { get; set; }
        public string AccountNo { get; set; }
        public int AmountValue { get; set; }
        public string CurrencyCode { get; set; }
        public string Rmks { get; set; }
        public string ReasonCode { get; set; }
    }
}
