using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
