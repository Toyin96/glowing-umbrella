using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;

namespace LegalSearch.Application.Validations.Commons
{
    public class EscalateRequestValidator : AbstractValidator<EscalateRequest>
    {
        public EscalateRequestValidator()
        {
            RuleFor(request => request.RecipientType)
                .NotEmpty().WithMessage("Recipient type is required.");

            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");
        }
    }
}
