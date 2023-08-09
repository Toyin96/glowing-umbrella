using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Exceptions;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace LegalSearch.Api.Middlewares
{
    /// <summary>
    /// Configures A Global Exception Handler To Handle Uncaught Exceptions
    /// </summary>
    public static class GlobalExceptionHandlerMiddleware
    {
        /// <summary>
        /// Extension Method Handling Global Exceptions
        /// </summary>
        /// <param name="app"></param>
        public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger>();
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (contextFeature != null)
                    {
                        var exception = contextFeature.Error.GetBaseException();

                        logger.LogCritical(exception, "Something went wrong: {Exception}", contextFeature.Error);

                        var responseMessage = exception switch
                        {
                            InAppException => contextFeature.Error.Message,
                            _ => "Error Processing Request"
                        };

                        await context.Response.WriteAsync(
                            new StatusResponse(responseMessage, ResponseCodes.ServiceError).Serialize());
                    }
                });
            });
        }
    }
}
