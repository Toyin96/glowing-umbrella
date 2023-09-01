namespace LegalSearch.Application.Models.Responses.CSO
{
    public class CsoRootResponsePayload
    {
        public List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public int TotalRequests { get; set; }
        public int WithinSLACount { get; set; }
        public int ElapsedSLACount { get; set; }
        public int Within3HoursToDueCount { get; set; }
        public int RequestsWithLawyersFeedbackCount { get; set; }
    }
}
