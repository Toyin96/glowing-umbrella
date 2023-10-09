namespace LegalSearch.Application.Models.Requests
{
    public class FinacleLegalSearchRequest
    {
        public required string BranchId { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string CustomerAccountNumber { get; set; }
    }
}
