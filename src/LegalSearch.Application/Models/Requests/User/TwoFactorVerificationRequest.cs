namespace LegalSearch.Application.Models.Requests.User
{
    public class TwoFactorVerificationRequest
    {
        public required string Email { get; set; }
        public required string TwoFactorCode { get; set; }
    }
}
