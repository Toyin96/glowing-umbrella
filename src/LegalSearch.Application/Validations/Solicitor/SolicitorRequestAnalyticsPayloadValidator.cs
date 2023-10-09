using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class SolicitorRequestAnalyticsPayloadValidator : AbstractValidator<SolicitorRequestAnalyticsPayload>
    {
        public SolicitorRequestAnalyticsPayloadValidator()
        {
            RuleFor(request => request.RequestStatus)
                .IsInEnum().WithMessage("Invalid request status value.");
        }
    }
}
