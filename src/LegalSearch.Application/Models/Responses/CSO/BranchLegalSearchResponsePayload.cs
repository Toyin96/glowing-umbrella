namespace LegalSearch.Application.Models.Responses.CSO
{
    public class BranchLegalSearchResponsePayload
    {
        public List<LegalSearchResponsePayload> LegalSearchRequests { get; set; }
        public List<MonthlyRequestData> RequestsCountBarChart { get; set; }
        public int CompletedRequestsCount { get; set; }
        public int OpenRequestsCount { get; set; }
    }
}
