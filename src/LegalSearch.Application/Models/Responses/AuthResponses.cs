namespace LegalSearch.Application.Models.Responses
{
    public sealed record LoginResponse
    {
        public Guid Id { get; set; }
        public string? Token { get; init; }
        public bool Is2FaRequired { get; set; }
        public string? DisplayName { get; set; }
        public string? Branch { get; set; }
        public string? Role { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? SolId { get; set; }
    }
}
