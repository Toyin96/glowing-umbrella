using FluentValidation;
using LegalSearch.Application.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Auth
{
    public class SolicitorOnboardRequestValidator : AbstractValidator<SolicitorOnboardRequest>
    {
        public SolicitorOnboardRequestValidator()
        {
            // Include validation for properties from BaseUserRequest
            Include(new BaseUserRequestValidator());

            RuleFor(request => request.FirstName)
                .NotEmpty().WithMessage("Please provide first name.");

            RuleFor(request => request.LastName)
                .NotEmpty().WithMessage("Please provide last name.");

            RuleFor(request => request.Firm)
                .NotNull().WithMessage("Firm details are required.");

            RuleFor(request => request.PhoneNumber)
                .NotEmpty().WithMessage("Please provide phone number.");

            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("Please provide email.")
                .EmailAddress().WithMessage("Please provide a valid email address.");

            RuleFor(request => request.BankAccount)
                .NotEmpty().WithMessage("Please provide bank account.");
        }
    }
}
