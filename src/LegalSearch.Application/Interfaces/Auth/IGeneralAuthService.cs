﻿using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.User;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IGeneralAuthService<User> : IUserAuthService<Domain.Entities.User.User>
    {
        Task<ObjectResponse<SolicitorOnboardResponse>> OnboardSolicitorAsync(SolicitorOnboardRequest request);
        Task<ObjectResponse<LoginResponse>> UserLogin(LoginRequest request);
        Task<ObjectResponse<ReIssueTokenResponse>> ReIssueToken(string userId);
        Task<StatusResponse> RequestUnlockCode(RequestUnlockCodeRequest request);
        Task<StatusResponse> UnlockCode(UnlockAccountRequest request);
        Task<ObjectResponse<LoginResponse>> Verify2fa(TwoFactorVerificationRequest request);
        Task<StatusResponse> ResetPassword(ResetPasswordRequest request);
        Task<StatusResponse> ChangePassword(ChangePasswordRequest request);
        Task<StatusResponse> OnboardNewUser(OnboardNewUserRequest request);
    }
}
