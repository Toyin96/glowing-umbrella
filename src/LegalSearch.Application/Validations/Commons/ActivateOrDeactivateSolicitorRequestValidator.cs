using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;

namespace LegalSearch.Application.Validations.Commons
{
    public class ActivateOrDeactivateSolicitorRequestValidator : AbstractValidator<ActivateOrDeactivateSolicitorRequest>
    {
        public ActivateOrDeactivateSolicitorRequestValidator()
        {
            RuleFor(request => request.SolicitorId)
                .NotEmpty().WithMessage("Solicitor ID is required.");

            RuleFor(request => request.ActionType)
                .IsInEnum().WithMessage("Invalid action type.");
        }
    }
}
