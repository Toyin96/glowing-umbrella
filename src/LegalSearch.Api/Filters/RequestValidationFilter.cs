using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LegalSearch.Api.Filters
{
    public class RequestValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var modelState = context.ModelState;

            if (modelState.IsValid) return;

            var entry = modelState.First();

            var hasJsonConversionError = entry.Value.Errors.Any(err =>
                err.ErrorMessage.Contains("json value could not be converted",
                    StringComparison.InvariantCultureIgnoreCase));

            var error = hasJsonConversionError
                ? "Please Verify JSON Payload Body"
                : entry.Value.Errors.First().ErrorMessage;

            var errors = modelState.Values.SelectMany(x => x.Errors.Select(c => c.ErrorMessage));

            var response = new ObjectResponse<IEnumerable<string>>(error,
                ResponseCodes.RequestValidationError)
            {
                Data = errors
            };

            context.Result = new BadRequestObjectResult(response);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
