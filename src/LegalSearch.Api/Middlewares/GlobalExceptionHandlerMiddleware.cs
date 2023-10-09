using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Exceptions;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using System.Net;

namespace LegalSearch.Api.Middlewares
{
    /// <summary>
    /// Configures A Global Exception Handler To Handle Uncaught Exceptions
    /// </summary>
    public static class GlobalExceptionHandlerMiddleware
    {
        public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(app =>
            {
                app.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (contextFeature != null)
                    {
                        var exception = contextFeature.Error.GetBaseException();

                        logger.LogError(exception, "An unhandled exception occurred: {Exception}", exception);

                        var response = new StatusResponse("An error occurred while processing your request.", ResponseCodes.ServiceError);

                        var jsonResponse = JsonConvert.SerializeObject(response);
                        await context.Response.WriteAsync(jsonResponse);
                    }
                });
            });
        }
    }
}
