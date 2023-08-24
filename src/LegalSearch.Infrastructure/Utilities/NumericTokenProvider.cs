using Microsoft.AspNetCore.Identity;

namespace LegalSearch.Infrastructure.Utilities
{
    public class NumericTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }

        public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var token = new Random().Next(1000, 9999).ToString("D4"); // Generate a 4-digit numeric token
            return Task.FromResult(token);
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            if (int.TryParse(token, out int numericToken) && numericToken >= 1000 && numericToken <= 9999)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

}
