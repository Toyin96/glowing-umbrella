using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using Microsoft.AspNetCore.Http;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class SubmitLegalSearchReportValidator : AbstractValidator<SubmitLegalSearchReport>
    {
        public SubmitLegalSearchReportValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleForEach(request => request.RegistrationDocuments)
                .Must(BeAValidIFormFile).WithMessage("Invalid registration document format.");
        }

        private bool BeAValidIFormFile(IFormFile file)
        {
            return file != null;
        }
    }
}
