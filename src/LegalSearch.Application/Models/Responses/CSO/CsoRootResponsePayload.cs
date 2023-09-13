namespace LegalSearch.Application.Models.Responses.CSO
{
    public class CsoRootResponsePayload
    {
        public List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public Dictionary<string, Dictionary<string, int>> RequestsCountBarChart { get; set; }
        public int PendingRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int OpenRequests { get; set; }
        public int TotalRequests { get; set; }
        public int WithinSLACount { get; set; }
        public int ElapsedSLACount { get; set; }
        public int Within3HoursToSLACount { get; set; }
        public int RequestsWithLawyersFeedbackCount { get; set; }
    }
}
