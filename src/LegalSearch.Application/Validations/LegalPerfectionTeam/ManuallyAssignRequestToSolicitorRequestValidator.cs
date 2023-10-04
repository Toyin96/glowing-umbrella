using FluentValidation;
using LegalSearch.Application.Models.Requests.LegalPerfectionTeam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.LegalPerfectionTeam
{
    public class ManuallyAssignRequestToSolicitorRequestValidator : AbstractValidator<ManuallyAssignRequestToSolicitorRequest>
    {
        public ManuallyAssignRequestToSolicitorRequestValidator()
        {
            RuleFor(request => request.SolicitorId)
                .NotEmpty().WithMessage("Solicitor ID is required.");

            RuleFor(request => request.RequestId)
                .NotEmpty().WithMessage("Request ID is required.");
        }
    }
}
