using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LegalSearch.Infrastructure.Services.Auth
{
    public class JwtTokenGenerator // : IAuthTokenGenerator
    {
        private readonly ILogger<JwtTokenGenerator> logger;
        private readonly string jwtKey;
        public const string ApiIssuer = "auto-post-api";

        public JwtTokenGenerator(ILogger<JwtTokenGenerator> logger, IConfiguration configuration)
        {
            this.logger = logger;
            // todo: implement a more secure way to handle this.
            jwtKey = configuration["JWTConfig:Key"];
        }

        public string Generate(UserSession session, TimeSpan validity)
        {
            logger.LogInformation("Generating User Auth JWT Token...");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(nameof(UserSession.UserId), session.UserId),
                    new Claim(nameof(UserSession.Name), session.Name),
                    new Claim(nameof(UserSession.DisplayName), session.DisplayName),
                    new Claim(nameof(UserSession.PhoneNumber), session.PhoneNumber),
                    new Claim(nameof(UserSession.Email), session.Email),
                    new Claim(nameof(UserSession.Department), session.Department),
                    new Claim(nameof(UserSession.BranchId), session.BranchId),
                    new Claim(nameof(UserSession.Sol), session.Sol),
                }),
                Expires = DateTime.UtcNow.Add(validity),
                Issuer = ApiIssuer,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }

        public string Generate(StaffSession session, TimeSpan validity)
        {
            logger.LogInformation("Generating Staff Auth JWT Token...");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(nameof(StaffSession.UserId), session.UserId ?? string.Empty),
                    new Claim(nameof(StaffSession.Name), session.Name ?? string.Empty),
                    new Claim(nameof(StaffSession.DisplayName), session.DisplayName ?? string.Empty),
                    new Claim(nameof(StaffSession.PhoneNumber), session.PhoneNumber ?? string.Empty),
                    new Claim(nameof(StaffSession.Email), session.Email ?? string.Empty),
                    new Claim(nameof(StaffSession.Department), session.Department ?? string.Empty),
                    new Claim(nameof(StaffSession.ManagerName), session.ManagerName ?? string.Empty),
                    new Claim(nameof(StaffSession.ManagerDepartment), session.ManagerDepartment ?? string.Empty),
                    new Claim(nameof(StaffSession.BranchId), session.BranchId ?? string.Empty),
                    new Claim(nameof(StaffSession.Groups), session.Groups ?? string.Empty),
                    new Claim(nameof(StaffSession.Sol), session.Sol ?? string.Empty),
                }),
                Expires = DateTime.UtcNow.Add(validity),
                Issuer = ApiIssuer,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }

    }
}
