namespace LegalSearch.Application.Models.Requests
{
    public class NameInquiryRequest
    {
        public required string RequestId { get; set; }
        public required string AccountNumber { get; set; }
    }
}
