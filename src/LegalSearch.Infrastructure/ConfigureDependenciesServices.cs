using LegalSearch.Application.Interfaces.Auth;
using LegalSearch.Infrastructure.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace LegalSearch.Infrastructure
{
    public static class ConfigureDependenciesServices
    {
        public static void ConfigureThirdPartyServices(this IServiceCollection services)
        {
            //services.AddTransient<IAuthService, AuthService>();

            //services.AddTransient<IAuthTokenGenerator, JwtTokenGenerator>();
        }
    }
}
