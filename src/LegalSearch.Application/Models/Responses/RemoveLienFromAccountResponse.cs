namespace LegalSearch.Application.Models.Responses
{
    public class RemoveLienFromAccountResponse
    {
        public bool Status { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public RemoveLienFromAccountResponseData? Data { get; set; }
    }

    public class RemoveLienFromAccountResponseData
    {
        public string? LienId { get; set; }
        public string? AccountId { get; set; }
    }
}
