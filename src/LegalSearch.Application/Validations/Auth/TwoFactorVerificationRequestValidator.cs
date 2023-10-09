using FluentValidation;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Validations.Auth
{
    public class TwoFactorVerificationRequestValidator : AbstractValidator<TwoFactorVerificationRequest>
    {
        public TwoFactorVerificationRequestValidator()
        {
            RuleFor(x => x.TwoFactorCode).NotNull().NotEmpty().Must(x => x.Length == 6).WithMessage("Please enter a valid two factor authenticator code");
        }
    }
}
