using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using Microsoft.AspNetCore.Http;

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
