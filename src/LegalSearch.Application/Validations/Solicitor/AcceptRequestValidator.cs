using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
