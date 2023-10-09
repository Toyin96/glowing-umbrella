using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests
{
    public class FCMBServiceAppConfig
    {
        [Required]
        public required string? BaseUrl { get; set; }
        [Required]
        public required string? ClientId { get; set; }
        [Required]
        public required string? Password { get; set; }
        [Required]
        public required string? SubscriptionKey { get; set; }
        public required string SLAPeriod { get; set; }
        public required string LegalSearchPaymentRemarks { get; set; }
        public required string CurrencyCode { get; set; }
        public required string LegalSearchAmount { get; set; }
        public required string LegalSearchRemarks { get; set; }
        public required string LegalSearchReasonCode { get; set; }
    }
}
