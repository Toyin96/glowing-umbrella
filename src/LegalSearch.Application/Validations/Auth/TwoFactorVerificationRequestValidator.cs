using FluentValidation;
using LegalSearch.Application.Models.Requests.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
