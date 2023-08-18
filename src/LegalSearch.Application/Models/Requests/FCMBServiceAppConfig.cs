using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests
{
    public class FCMBServiceAppConfig
    {
        [Required]
        public string BaseUrl { get; set; }
        [Required]
        public string ClientId { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string SubscriptionKey { get; set; }
        public string SLAPeriod { get; set; }
        public string LegalSearchAmount { get; set; }
        public string LegalSearchRemarks { get; set; }
        public string LegalSearchReasonCode { get; set; }
    }
}
