namespace LegalSearch.Application.Models.Responses.CSO
{
    public class FinacleLegalSearchResponsePayload
    {
        public Guid RequestId { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string RequestStatus { get; set; }
        public required string AccountBranchName { get; set; } // branch account was opened
        public required string CustomerAccountNumber { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
