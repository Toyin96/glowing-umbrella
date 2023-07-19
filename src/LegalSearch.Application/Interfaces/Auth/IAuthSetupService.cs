using System.Threading.Tasks;
using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IAuthSetupService
    {
        Task<ObjectResponse<LoginResponse>> LoginAsync(LoginRequest request);
        Task<StatusResponse> GuestLoginAsync(LoginRequest request);
    }
}
