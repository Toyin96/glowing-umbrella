namespace LegalSearch.Application.Models.Requests.User
{
    public class TwoFactorVerificationRequest
    {
        public string Email { get; set; }
        public string TwoFactorCode { get; set; }
    }
}
