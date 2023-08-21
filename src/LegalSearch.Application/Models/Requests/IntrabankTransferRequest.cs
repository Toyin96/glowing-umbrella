namespace LegalSearch.Application.Models.Requests
{
    public class IntrabankTransferRequest
    {
        public string DebitAccountNo { get; set; }
        public string CreditAccountNo { get; set; }
        public bool IsFees { get; set; }
        public List<Charge> Charges { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Narration { get; set; }
        public string Remark { get; set; }
        public string CustomerReference { get; set; }
    }

    public class Charge
    {
        public string Account { get; set; }
        public decimal Fee { get; set; }
    }
}
