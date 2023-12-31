﻿using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace LegalSearch.Api.Controllers
{
    [ExcludeFromCodeCoverage]
    public class BaseController : ControllerBase
    {
        protected ActionResult<T> HandleResponse<T>(T result) where T : StatusResponse
        {
            return result.Code switch
            {
                ResponseCodes.Success => Ok(result),
                ResponseCodes.NotFound => NotFound(result),
                ResponseCodes.Forbidden => Unauthorized(result),
                ResponseCodes.Unauthenticated => Unauthorized(result),
                ResponseCodes.ServiceError => StatusCode(500, result),
                ResponseCodes.DataNotFound => NotFound(result),
                _ => BadRequest(result)
            };
        }
    }
}
