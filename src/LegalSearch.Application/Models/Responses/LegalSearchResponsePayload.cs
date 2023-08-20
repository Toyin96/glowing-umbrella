namespace LegalSearch.Application.Models.Responses
{
    public class LegalSearchResponsePayload
    {
        public required string RequestInitiator { get; set; }
        public required string RequestType { get; set; }
        public required string CustomerAccountName { get; set; }
        public required string RequestStatus { get; set; }
        public required string CustomerAccountNumber { get; set; }
        public required string BusinessLocation { get; set; }
        public required string RegistrationLocation { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateDue { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class LegalSearchRootResponsePayload
    {
        public required List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public int TotalRequests { get; set; }
        public int WithinSLACount { get; set; }
        public int ElapsedSLACount { get; set; }
        public int Within3HoursToDueCount { get; set; }
    }
}
