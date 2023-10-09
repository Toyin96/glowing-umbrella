using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LegalSearch.Api.Filters
{
    /// <summary>
    /// Action filter for request validation.
    /// </summary>
    public class RequestValidationFilter : IActionFilter
    {
        /// <summary>
        /// Executed before the action method is invoked, validates the request model.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var modelState = context.ModelState;

            // Check if the model state is valid
            if (modelState.IsValid)
                return;

            // Get the first model state entry
            var entry = modelState.First();

            // Check if there was a JSON conversion error
            var hasJsonConversionError = entry.Value.Errors.Any(err =>
                err.ErrorMessage.Contains("json value could not be converted", StringComparison.InvariantCultureIgnoreCase));

            // Determine the error message
            var error = hasJsonConversionError
                ? "Please verify JSON payload body"
                : entry.Value.Errors.First().ErrorMessage;

            // Collect all errors from model state
            var errors = modelState.Values.SelectMany(x => x.Errors.Select(c => c.ErrorMessage));

            // Create a response object with the appropriate error and status code
            var response = new ObjectResponse<IEnumerable<string>>(error, ResponseCodes.RequestValidationError)
            {
                Data = errors
            };

            // Set the result to a BadRequestObjectResult with the response
            context.Result = new BadRequestObjectResult(response);
        }

        /// <summary>
        /// Executed after the action method is invoked, does nothing in this implementation.
        /// </summary>
        /// <param name="context">The action executed context.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action is taken after the action method is invoked
        }
    }
}
