﻿using System;
using System.Reflection;
using Fcmb.Shared.Models.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.ConfigureThirdPartyServices();
            
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });
            
            services.ConfigureHttpClients(configuration);
        }

        private static void ConfigureHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureAuthHttpClient(services, configuration);
        }
        
        private static void ConfigureAuthHttpClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpConstants.AuthHttpClient, client =>
            {
                var baseUrl = configuration["FCMBConfig:BaseUrl"]!;
                // todo: look into more secure way of handling these sensitive info...
                var clientId = configuration["FCMBConfig:ClientId"];
                var subscriptionKey = configuration["FCMBConfig:SubscriptionKey"];

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                client.DefaultRequestHeaders.Add("client_id", clientId);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            });
        }

        private static void AutoInjectService(this IServiceCollection services)
        {
            //Register Services with Interface
            services.Scan(scan => scan.FromCallingAssembly().AddClasses(classes => classes
                    .Where(type => type.Name.EndsWith("Service") && type.GetInterfaces().Length > 0), false)
                .AsSelfWithInterfaces()
                .WithTransientLifetime());
        }
    }
}