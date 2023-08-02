using System.Security.Claims;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IJwtTokenService
    {
        string GenerateJwtToken(ClaimsIdentity identity);
        ClaimsPrincipal ValidateJwtToken(string token);
    }
}
