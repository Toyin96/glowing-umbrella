using FluentValidation;
using LegalSearch.Application.Models.Requests.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
