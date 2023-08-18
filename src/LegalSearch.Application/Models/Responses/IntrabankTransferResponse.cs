namespace LegalSearch.Application.Models.Responses
{
    public class IntrabankTransferResponse
    {
        public IntrabankTransferResponseData Data { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
    }

    public class IntrabankTransferResponseData
    {
        public string Stan { get; set; }
        public string CustomerReference { get; set; }
        public string Amount { get; set; }
        public string TranId { get; set; }
    }
}
