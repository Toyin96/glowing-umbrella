using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;

namespace LegalSearch.Api.Filters
{
    /// <summary>
    /// Action filter for FluentValidation-based request model validation.
    /// </summary>
    public class FluentValidationFilter : IAsyncActionFilter
    {
        private readonly IValidatorFactory _validatorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentValidationFilter"/> class.
        /// </summary>
        /// <param name="validatorFactory">The validator factory to obtain validators.</param>
        public FluentValidationFilter(IValidatorFactory validatorFactory)
        {
            _validatorFactory = validatorFactory;
        }

        /// <summary>
        /// Executes asynchronously before the action method is invoked, validates the request models using FluentValidation.
        /// </summary>
        /// <param name="context">The action executing context.</param>
        /// <param name="next">The delegate to continue the execution.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var validationErrors = new List<string>();

            foreach (var (key, value) in context.ActionArguments)
            {
                if (value == null)
                {
                    // Skip null models
                    continue;
                }

                var validator = _validatorFactory.GetValidator(value.GetType());

                if (validator == null)
                {
                    // No validator found for the model
                    continue;
                }

                var validationResult = await validator.ValidateAsync(new ValidationContext<object>(value));

                if (!validationResult.IsValid)
                {
                    validationErrors.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));
                }
            }

            if (validationErrors.Any())
            {
                var response = new ObjectResponse<IEnumerable<string>>("The request model is not valid", ResponseCodes.RequestValidationError)
                {
                    Data = validationErrors
                };

                context.Result = new BadRequestObjectResult(response);
                return;
            }

            // Continue with the action execution
            await next();
        }
    }
}
