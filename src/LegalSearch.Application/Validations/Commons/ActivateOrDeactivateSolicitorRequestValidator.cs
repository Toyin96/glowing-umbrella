using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
