namespace LegalSearch.Application.Models.Responses
{
    public class AddLienToAccountResponse
    {
        public bool Status { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public AddLienToAccountResponseData? Data { get; set; }
    }

    public class AddLienToAccountResponseData
    {
        public string? LienId { get; set; }
        public string? AccountId { get; set; }
    }
}
