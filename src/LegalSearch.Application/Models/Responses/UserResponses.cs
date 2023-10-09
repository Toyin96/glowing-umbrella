namespace LegalSearch.Application.Models.Responses
{
    public record SolicitorOnboardResponse
    {
        public Guid SolicitorId { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string Email { get; init; }
        public required string PhoneNumber { get; init; }
        public required string Address { get; init; }
        public required string AccountNumber { get; init; }
        public required string Firm { get; init; }
        public required string State { get; init; }
    }
}
