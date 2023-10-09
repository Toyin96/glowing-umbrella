using FluentValidation;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Validations.Auth
{
    internal class UnlockAccountRequestValidator : AbstractValidator<UnlockAccountRequest>
    {
        public UnlockAccountRequestValidator()
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Please enter a valid email address");
            RuleFor(x => x.UnlockCode).NotEmpty().NotNull().WithMessage("Please enter a valid unlock code");
        }
    }
}
