using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests
{
    public class FCMBConfig
    {
        public required string BaseUrl { get; set; }
        public required string ApplicationBaseUrl { get; set; }
        public required string ClientId { get; set; }
        public required string SecretKey { get; set; }
        public required string FrontendBaseUrl { get; set; }
        public required string SubscriptionKey { get; set; }
        public required string Password { get; set; }
        public required string SLAPeriod { get; set; }
        public required string CurrencyCode { get; set; }
        public required string LegalSearchAmount { get; set; }
        public required string LegalSearchRemarks { get; set; }
        public required string LegalSearchPaymentRemarks { get; set; }
        public required string LegalSearchReasonCode { get; set; }
        public required AuthConfig AuthConfig { get; set; }
        public required EmailConfig EmailConfig { get; set; }
    }

    public class EmailConfig
    {
        public required string EmailUrl { get; set; }
        public required string SenderEmail { get; set; }
        public required string SenderName { get; set; }
    }

    public class AuthConfig
    {
        public required string AuthUrl { get; set; }
        public required string AuthClientId { get; set; }
        public required string AuthSecretKey { get; set; }
    }
}
