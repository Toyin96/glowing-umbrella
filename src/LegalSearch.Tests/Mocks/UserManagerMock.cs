using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalSearch.Tests.Mocks
{
    public static class UserManagerMock
    {
        public static UserManager<TUser> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            return new UserManager<TUser>(store.Object, null, null, null, null, null, null, null, null);
        }
    }
}
