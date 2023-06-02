namespace LegalSearch.Application.Models.Responses
{
    public sealed record LoginResponse
    {
        public string Token { get; init; }
    }
}
