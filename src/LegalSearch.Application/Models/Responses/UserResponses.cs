using System;

namespace LegalSearch.Application.Models.Responses
{
    public record SolicitorOnboardResponse
    {
        public Guid SolicitorId { get; init; }
        public string FirstName { get; init; } 
        public string LastName { get; init; }
        public string Email { get; init; }
        public string PhoneNumber { get; init; }
        public string Address { get; init; }
        public string AccountNumber { get; init; }
        public string Firm { get; init; }
        public string State { get; init; }
    }
}
