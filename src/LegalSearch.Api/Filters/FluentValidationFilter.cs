using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Constants;

namespace LegalSearch.Api.Filters
{
    public class FluentValidationFilter : IAsyncActionFilter
    {
        private readonly IValidatorFactory _validatorFactory;

        public FluentValidationFilter(IValidatorFactory validatorFactory)
        {
            _validatorFactory = validatorFactory;
        }

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

            await next();
        }
    }
}
