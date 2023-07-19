using System.Threading.Tasks;
using LegalSearch.Application.Models.Auth;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface ISessionService
    {
        UserSession? GetUserSession();
        StaffSession? GetStaffSession();
        Task<bool> HasPermissionAsync(params string[] permissions);
    }
}
