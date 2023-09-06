namespace LegalSearch.Application.Models.Responses.Solicitor
{
    public class SolicitorProfileDto
    {
        public Guid SolicitorId { get; set; }
        public required string SolicitorName { get; set; }
        public required string Firm { get; set; }
        public Guid FirmId { get; set; }
        public required string SolicitorEmail { get; set; }
        public required string SolicitorPhoneNumber { get; set; }
        public required string SolicitorState { get; set; }
        public Guid SolicitorStateId { get; set; }
        public Guid SolicitorStateOfCoverageId { get; set; }
        public required string SolicitorRegion { get; set; }
        public required string SolicitorAddress { get; set; }
        public required string Status { get; set; }
    }
}
