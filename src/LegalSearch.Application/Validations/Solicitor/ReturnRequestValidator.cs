using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class ReturnRequestValidator : AbstractValidator<ReturnRequest>
    {
        public ReturnRequestValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleForEach(request => request.SupportingDocuments)
                .Must(BeAValidIFormFile).WithMessage("Invalid supporting document format.");
        }

        private bool BeAValidIFormFile(IFormFile file)
        {
            return file != null;
        }
    }
}
