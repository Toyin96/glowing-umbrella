﻿using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;

namespace LegalSearch.Application.Validations.CSO
{
    public class UpdateFinacleLegalRequestValidator : AbstractValidator<UpdateFinacleLegalRequest>
    {
        public UpdateFinacleLegalRequestValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleFor(request => request.RequestType)
                .NotEmpty().WithMessage("Request type is required.");

            RuleFor(request => request.BusinessLocation)
                .NotEmpty().WithMessage("Business location is required.");

            RuleFor(request => request.RegistrationLocation)
                .NotEmpty().WithMessage("Registration location is required.");

            RuleFor(request => request.CustomerAccountName)
                .NotEmpty().WithMessage("Customer account name is required.");

            RuleFor(request => request.CustomerAccountNumber)
                .NotEmpty().WithMessage("Customer account number is required.");

            RuleFor(request => request.RegistrationNumber)
                .NotEmpty().WithMessage("Registration number is required.");

            RuleFor(request => request.RegistrationDate)
                .NotEmpty().WithMessage("Registration date is required.");

            RuleFor(request => request.RegistrationDocuments)
                .NotEmpty().WithMessage("Registration documents are required.");
        }
    }
}
