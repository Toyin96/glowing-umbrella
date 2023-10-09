namespace LegalSearch.Application.Models.Requests
{
    public class IntrabankTransferRequest
    {
        public required string DebitAccountNo { get; set; }
        public required string CreditAccountNo { get; set; }
        public bool IsFees { get; set; }
        public List<Charge>? Charges { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public required string Narration { get; set; }
        public required string Remark { get; set; }
        public required string CustomerReference { get; set; }
    }

    public class Charge
    {
        public required string Account { get; set; }
        public decimal Fee { get; set; }
    }
}
