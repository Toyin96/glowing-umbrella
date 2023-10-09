using System.Text.Json.Serialization;

namespace LegalSearch.Application.Models.Responses
{
    public class GetAccountInquiryResponse
    {
        public GetAccountInquiryResponseData? Data { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
    }

    public class GetAccountInquiryResponseData
    {
        public string? CustomerId { get; set; }
        public string? AccountStatus { get; set; }
        [JsonPropertyName("biometric_ID")]
        public object? BiometricID { get; set; }
        [JsonPropertyName("rC_No")]
        public string? RCNo { get; set; }
        [JsonPropertyName("frez_code")]
        public string? FrezCode { get; set; }
        public string? AccountNumber { get; set; }
        public string? Currency { get; set; }
        public string? LienAmount { get; set; }
        public string? FreezeReason { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal LedgerBalance { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public string? AccountName { get; set; }
        public string? MobileNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Address1 { get; set; }
        public string? Sex { get; set; }
        public string? AccountOfficerCode { get; set; }
        public string? EmployerId { get; set; }
        public string? IsMinor { get; set; }
        public string? PassportNumber { get; set; }
        public string? BranchCode { get; set; }
        public string? DateOfBirth { get; set; }
        public object? DateOfIncorporation { get; set; }
        public string? SchemeCode { get; set; }
        public string? SbuCode { get; set; }
        public string? BrokerCode { get; set; }
        public object? AccountShortName { get; set; }
        public object? GlSubHeadCode { get; set; }
        public decimal LegalSearchAmount { get; set; }
    }
}
