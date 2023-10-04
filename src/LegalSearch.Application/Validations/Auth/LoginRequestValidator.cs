using Fcmb.Shared.Auth.Models.Requests;
using FluentValidation;

namespace LegalSearch.Application.Validations.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().NotNull().EmailAddress().WithMessage("Please enter a valid email address");
        }
    }
}
