namespace LegalSearch.Application.Models.Requests.LegalPerfectionTeam
{
    public class ManuallyAssignRequestToSolicitorRequest
    {
        public Guid SolicitorId { get; set; }
        public Guid RequestId { get; set; }
    }
}
