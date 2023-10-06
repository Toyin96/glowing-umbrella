using FluentValidation;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using HealthChecks.UI.Client;
using LegalSearch.Api.HealthCheck;
using LegalSearch.Api.Logging;
using LegalSearch.Api.Middlewares;
using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Models.Logging;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Validations.Auth;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Enums.Role;
using LegalSearch.Infrastructure.BackgroundJobs;
using LegalSearch.Infrastructure.Persistence;
using LegalSearch.Infrastructure.Services.FCMB;
using LegalSearch.Infrastructure.Services.Notification;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
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
                //options.Filters.Add<FluentValidationFilter>();
            }).AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // Keep camelCase
            });
            services.ConfigureAuthentication(configuration);
            services.AddRouting();
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.RegisterHttpRequiredServices(configuration);
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();

            // Register custom health checks
            services.AddHealthChecks()
                    .AddCheck<ApiHealthCheck>("api_health_check")
                    .AddCheck<SecurityHealthCheck>("security_health_check")
                    .AddCheck<DatabaseHealthCheck>("database_health_check");

            // add logging capabilities
            // Retrieve logger options from appsettings.json
            var loggerOptions = configuration.GetSection("Logging").Get<LoggerOptions>();
            services.ConfigureLoggingCapability(configuration, loggerOptions);

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://20.161.50.136:5007", "http://localhost:8080")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            // added signalR capability
            services.AddSignalR(option => option.EnableDetailedErrors = true);

            services.AddHttpClient<IFCMBService, FCMBService>();
            services.AddOptions<FCMBServiceAppConfig>()
                    .BindConfiguration(nameof(FCMBServiceAppConfig))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            //Configure database
            services.ConfigureDatabase(configuration);

            //Configure hangfire
            services.ConfigureHangFire(configuration);

            services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Transient);

            //Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configuration">The configuration.</param>
        public static void ConfigureHttpRequestPipeline(this WebApplication app, IConfiguration configuration)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LegalSearch.Api v1"));
            }

            app.UseCors();

            app.UseGlobalExceptionHandler();

            app.UseRouting();

            app.MapHub<NotificationHub>("/notificationHub");

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            UpdateDatabase(app, configuration); // ensure migration upon startup

            app.UseAuthentication();

            app.UseAuthorization();

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

            app.UseHealthChecksUI(options => options.UIPath = "/health-ui");

            // Call the static method to register recurring Hangfire jobs
            HangfireJobs.RegisterRecurringJobs();

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

        private static void ConfigureLoggingCapability(this IServiceCollection services, IConfiguration configuration, LoggerOptions? loggerOptions)
        {
            // Check if loggerOptions is null
            if (loggerOptions == null)
            {
                throw new ArgumentNullException(nameof(loggerOptions), "LoggerOptions is null. Check the configuration.");
            }

            // Configure the logger
            var loggerConfigurationService = new LoggerConfigurationService(configuration);
            var loggerConfiguration = loggerConfigurationService.ConfigureLogger(loggerOptions);

            // Set the logger as the default logger for the application
            Log.Logger = loggerConfiguration.CreateLogger();

            // Clear existing logging providers and add Serilog
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });
        }
        private static void ConfigureHangFire(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHangfire(config =>
            {
                config.UseSimpleAssemblyNameTypeSerializer();
                config.UseRecommendedSerializerSettings();
                config.UseSqlServerStorage(configuration.GetConnectionString("legal_search_db"));
            });

            services.AddHangfireServer();
        }

        private static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Add database context
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("legal_search_db"), sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("LegalSearch.Infrastructure");
                    sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null); // Optional: Enable automatic retries on transient failures.
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

        private static void UpdateDatabase(IApplicationBuilder app, IConfiguration configuration)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<AppDbContext>();
                var userManager = serviceScope.ServiceProvider.GetService<UserManager<User>>();
                var roleManager = serviceScope.ServiceProvider.GetService<RoleManager<Role>>();

                context!.Database.Migrate(); // apply migration on startup
                context.Database.EnsureCreated();

                // seed default admin
                SeedAdmin(configuration, userManager!, roleManager!).Wait();
            }
        }

        /// <summary>
        /// This method seeds a default admin into the application db if not exists
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="userManager"></param>
        /// <param name="roleManager"></param>
        /// <returns></returns>
        public static async Task SeedAdmin(IConfiguration configuration, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            // seed all roles in application on startup
            foreach (RoleType roleType in Enum.GetValues(typeof(RoleType)))
            {
                var roleName = roleType.ToString();

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role { Name = roleName });
                }
            }


            if (!await roleManager.RoleExistsAsync(RoleType.Admin.ToString()))
            {
                await roleManager.CreateAsync(new Role { Name = nameof(RoleType.Admin) });
            }

            var adminUser = await userManager.FindByNameAsync(configuration["AdminSettings:Email"]);
            if (adminUser == null)
            {
                adminUser = new User { FirstName = configuration["AdminSettings:FirstName"], UserName = configuration["AdminSettings:Email"], Email = configuration["AdminSettings:Email"] };
                await userManager.CreateAsync(adminUser, configuration["AdminSettings:Password"]);
            }

            if (!await userManager.IsInRoleAsync(adminUser, nameof(RoleType.Admin)))
            {
                await userManager.AddToRoleAsync(adminUser, nameof(RoleType.Admin));
            }
        }

        public static void RegisterHttpRequiredServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpClient("notificationClient", client =>
            {
                client.BaseAddress = new Uri(configuration["FCMBServiceAppConfig:EmailServiceBaseAddress"]);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("client_id", configuration["FCMBServiceAppConfig:ClientId"]);
            });
        }
    }
}
