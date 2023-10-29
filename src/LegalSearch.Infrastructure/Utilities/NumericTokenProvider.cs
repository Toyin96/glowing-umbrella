using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace LegalSearch.Infrastructure.Utilities
{
    public class NumericTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class
    {
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }

        public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            byte[] data = new byte[4];
            _rng.GetBytes(data);  // Generate 4 random bytes

            int randomNumber = Math.Abs(BitConverter.ToInt32(data, 0)) % 9000 + 1000; // Map the bytes to a 4-digit number

            return Task.FromResult(randomNumber.ToString("D4"));
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
