using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalSearch.Tests.Mocks
{
    public static class MockUserManager
    {
        public static Mock<UserManager<TUser>> CreateMockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var userManager = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
            return userManager;
        }
    }
}
