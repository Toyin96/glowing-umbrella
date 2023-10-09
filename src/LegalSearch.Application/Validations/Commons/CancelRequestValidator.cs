using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;

namespace LegalSearch.Application.Validations.Commons
{
    public class CancelRequestValidator : AbstractValidator<CancelRequest>
    {
        public CancelRequestValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");

            RuleFor(request => request.Reason)
                .NotEmpty().WithMessage("Reason is required.");
        }
    }
}
