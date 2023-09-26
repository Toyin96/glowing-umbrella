namespace LegalSearch.Application.Models.Responses.ZSM
{
    public class ZonalServiceManagerReportModel
    {
        public int RequestsPendingWithCsoCount { get; set; }
        public int RequestsPendingWithSolicitorCount { get; set; }
        public int RequestsWithElapsedSlaCount { get; set; }
        public int CompletedRequestsCount { get; set; }
    }
}
