using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class AcceptRequestValidator : AbstractValidator<AcceptRequest>
    {
        public AcceptRequestValidator()
        {
            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");
        }
    }
}
