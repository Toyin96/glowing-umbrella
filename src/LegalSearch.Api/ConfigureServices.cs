using System.Reflection;
using System.Text;
using LegalSearch.Api.Filters;
using LegalSearch.Api.Middlewares;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
            services.ConfigureAuthentication(configuration);
            services.AddRouting(x => x.LowercaseUrls = true);
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();
            services.AddControllers(options =>
            {
                options.Filters.Add<RequestValidationFilter>();
            });

            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

            //configure database
            services.ConfigureDatabase();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.ConfigureSwagger();
        }

        /// <summary>
        /// Configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        public static void ConfigureHttpRequestPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LegalSearch.Api v1"));
            }

            app.UseRouting();

            app.UseHttpsRedirection();
            
            app.UseGlobalExceptionHandler();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
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
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                };
            });
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
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }
    }
}
