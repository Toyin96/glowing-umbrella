namespace LegalSearch.Application.Models.Requests.User
{
    public class ZonalServiceManagerMiniDto
    {
        public required Guid Id { get; set; }
        public required string EmailAddress { get; set; }
        public string? AlternateEmailAddress { get; set; }
        public required string Name { get; set; }
    }
}
