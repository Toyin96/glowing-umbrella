using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.LegalPerfectionTeam
{
    public class ViewSolicitorsBasedOnRegionRequestFilterValidator : AbstractValidator<ViewSolicitorsBasedOnRegionRequestFilter>
    {
        public ViewSolicitorsBasedOnRegionRequestFilterValidator()
        {
            RuleFor(request => request.Status)
                .IsInEnum().When(request => request.Status.HasValue)
                .WithMessage("Invalid profile status type.");
        }
    }
}
