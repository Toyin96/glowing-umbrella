using System.Net;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using LegalSearch.Application.Exceptions;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Api.Middlewares
{
    public static class GlobalExceptionHandlerMiddleware
    {
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
