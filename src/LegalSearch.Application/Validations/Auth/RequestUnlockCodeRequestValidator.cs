using FluentValidation;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Validations.Auth
{
    public class RequestUnlockCodeRequestValidator : AbstractValidator<RequestUnlockCodeRequest>
    {
        public RequestUnlockCodeRequestValidator()
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Please enter a valid email address");
        }
    }
}
