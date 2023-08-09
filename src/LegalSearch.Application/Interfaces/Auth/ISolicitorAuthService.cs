using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface ISolicitorAuthService<Solicitor> : IUserAuthService<Domain.Entities.User.User>
    {
        Task<ObjectResponse<SolicitorOnboardResponse>> OnboardSolicitorAsync(SolicitorOnboardRequest request);
        Task<ObjectResponse<LoginResponse>> SolicitorLogin(LoginRequest request);
    }
}
