﻿using Fcmb.Shared.Auth.Services;
using Fcmb.Shared.Models.Constants;
using LegalSearch.Application.Interfaces.Notification;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Notification;
using LegalSearch.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LegalSearch.Infrastructure
{
    public static class ConfigureServices
    {
        /// <summary>
        /// Configure Infrastructure services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void ConfigureInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AutoInjectService();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            //configuring identity & users
            services.ConfigureIdentity();
            services.ConfigureHttpClients(configuration);
        }

        public static void ConfigureHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureAuthHttpClient(services, configuration);
        }

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            static void SetupIdentityOptions(IdentityOptions x)
            {
                // x.User./
                x.User.RequireUniqueEmail = true;

                // NewPassword settings.
                x.Password.RequiredLength = 8;

                // Lockout settings.
                x.Lockout.MaxFailedAccessAttempts = 3;
                x.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                x.Lockout.AllowedForNewUsers = true;

                // Enable Two-Factor Authentication
                x.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
                x.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider;
                x.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                x.Tokens.PasswordResetTokenProvider = "NumericTokenProvider"; // Set the name of your NumericTokenProvider
            }

            services.AddIdentity<User, Role>(SetupIdentityOptions)
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddRoleManager<RoleManager<Role>>()
                    .AddSignInManager<SignInManager<User>>()
                    .AddDefaultTokenProviders()
                    .AddTokenProvider<NumericTokenProvider<User>>("NumericTokenProvider"); // default token provider for 2fa
        }

        public static void ConfigureAuthHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpConstants.AuthHttpClient, client =>
            {
                var baseUrl = configuration["FCMBConfig:AuthConfig:AuthUrl"]!;

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            });
        }

        public static void AutoInjectService(this IServiceCollection services)
        {
            //Register Services with Interface
            services.Scan(scan => scan.FromCallingAssembly().AddClasses(classes => classes
                    .Where(type => (type.Name.EndsWith("Service") || type.Name.EndsWith("Manager")) && type.GetInterfaces().Length > 0), false)
                .AsSelfWithInterfaces()
                .WithTransientLifetime());

            services.TryAddScoped<INotificationService, EmailNotificationService>();
            services.TryAddScoped<IAuthService, AuthService>();
            services.TryAddScoped<INotificationService, NotificationPersistenceService>();
        }
    }
}
