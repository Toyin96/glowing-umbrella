namespace LegalSearch.Application.Models.Requests.User
{
    public class UnlockAccountRequest
    {
        public required string Email { get; set; }
        public required string UnlockCode { get; set; }
    }
}
