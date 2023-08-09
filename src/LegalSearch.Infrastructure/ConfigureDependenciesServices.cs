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
