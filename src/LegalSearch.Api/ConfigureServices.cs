using Hangfire;
using HangfireBasicAuthenticationFilter;
using LegalSearch.Api.Filters;
using LegalSearch.Api.Middlewares;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.Notification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LegalSearch.Api
{
    public static class ConfigureServices
    {
        /// <summary>
        /// Add services to the container.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddServicesToContainer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
                // options.Filters.Add<RequestValidationFilter>();
            }).AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // Keep camelCase
            });
            services.ConfigureAuthentication(configuration);
            services.AddRouting();
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddSignalR(); // added signalR capability
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();

            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

            //configure database
            services.ConfigureDatabase();

            //configure hangfire
            services.ConfigureHangFire();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();
        }

        /// <summary>
        /// Configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        public static void ConfigureHttpRequestPipeline(this WebApplication app, IConfiguration configuration)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LegalSearch.Api v1"));
            }

            app.UseRouting();

            app.UseHttpsRedirection();

            app.UseGlobalExceptionHandler();

            app.MapHub<NotificationHub>("/notificationHub"); // Map the NotificationHub

            app.UseAuthentication();

            app.UseAuthorization();

            //app.UseMiddleware<RoleAuthorizationMiddleware>();

            app.MapControllers();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = configuration["HangfireConfig:DashboardTitle"],
                Authorization = new List<HangfireCustomBasicAuthenticationFilter>()
                {
                    new HangfireCustomBasicAuthenticationFilter()
                    {
                        Pass = configuration["HangfireConfig:Password"],
                        User = configuration["HangfireConfig:User"]
                    }
                }
            });

            app.MapHangfireDashboard();
        }

        /// <summary>
        /// Configures the JWT Authentication.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        private static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                };
            });
        }

        private static void ConfigureHangFire(this IServiceCollection services)
        {
            services.AddHangfire(config =>
            {
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSqlServerStorage(AppConstants.DbConnectionString + ";TrustServerCertificate=True");
            });

            services.AddHangfireServer();
        }

        private static void ConfigureDatabase(this IServiceCollection services)
        {
            // Add database context
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(AppConstants.DbConnectionString + ";TrustServerCertificate=True", sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LegalSearch.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(); // Optional: Enable automatic retries on transient failures.
                });
            });
        }

        private static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Legal Search API",
                    Contact = new OpenApiContact
                    {
                        Name = "SBSC",
                        Email = string.Empty,
                        Url = new Uri("https://www.sbsc.com/"),
                    }
                });
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };

                c.DescribeAllParametersInCamelCase();

                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {securityScheme, new string[]{} }
                });

                c.OperationFilter<SwaggerFileUploadFilter>();

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }
    }
}
