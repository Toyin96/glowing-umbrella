using System;
using System.Threading.Tasks;
using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Auth.Services;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Application.Models.Auth;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Responses;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Services.Auth
{
    public class AuthSetupService : IAuthSetupService
    {
        private readonly IAuthService authService;
        private readonly IAuthTokenGenerator authTokenGenerator;
        private readonly ILogger<AuthSetupService> logger;

        public AuthSetupService(IAuthService authService, ILogger<AuthSetupService> logger, IAuthTokenGenerator authTokenGenerator)
        {
            this.authService = authService;
            this.logger = logger;
            this.authTokenGenerator = authTokenGenerator;
        }

        public async Task<ObjectResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var result = await authService.LoginAsync(request);

            if (result.Code is ResponseCodes.Success)
            {
                logger.LogInformation("{Username} successfully logged in", request.Email);

                var session = GenerateUserSession(result.Data);
                var token = authTokenGenerator.Generate(session, TimeSpan.FromHours(1));
                
                return new ObjectResponse<LoginResponse>("Successfully Logged In User")
                {
                    Data = new LoginResponse
                    {
                        Token = token
                    }
                };
            }

            logger.LogWarning("{Username} unsuccessfully logged in", request.Email);
            return new ObjectResponse<LoginResponse>("Invalid Credentials", ResponseCodes.InvalidCredentials);
        }
        
        private static UserSession GenerateUserSession(AdLoginResponse loginResponse)
        {
            return new(loginResponse.StaffId, loginResponse.StaffName, loginResponse.DisplayName,
                loginResponse.MobileNo, loginResponse.Email, loginResponse.Department,
                loginResponse.BranchId, loginResponse.Sol);
        }
    }
}
