namespace LegalSearch.Application.Models.Requests.User
{
    public class UnlockAccountRequest
    {
        public string Email { get; set; }
        public string UnlockCode { get; set; }
    }
}
