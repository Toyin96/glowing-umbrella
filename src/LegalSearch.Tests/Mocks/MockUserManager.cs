using LegalSearch.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace LegalSearch.Tests.Mocks
{
    public class MockUserManager : UserManager<User>
    {
        public MockUserManager(IUserStore<User> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<User> passwordHasher, IEnumerable<IUserValidator<User>> userValidators, IEnumerable<IPasswordValidator<User>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<User>> logger)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }

        public override Task<User> FindByIdAsync(string userId)
        {
            // Implement your FindByIdAsync logic for testing
            if (Guid.TryParse(userId, out Guid newUserId))
            {
                return Task.FromResult(new User { Id = newUserId, FirstName = "testuser" });
            }

            return Task.FromResult((User)null); // Return null for invalid user
        }
    }
}
