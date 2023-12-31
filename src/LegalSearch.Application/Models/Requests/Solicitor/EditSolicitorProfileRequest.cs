﻿namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class EditSolicitorProfileByLegalTeamRequest
    {
        public Guid SolicitorId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string FirmName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required Guid State { get; set; }
        public required string Address { get; set; }
        public required string AccountNumber { get; set; }
    }
}
