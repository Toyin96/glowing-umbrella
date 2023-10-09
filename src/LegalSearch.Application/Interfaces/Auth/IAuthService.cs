using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Models.Responses;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<ObjectResponse<AdLoginResponse>> LoginAsync(LoginRequest request);
    }
}
