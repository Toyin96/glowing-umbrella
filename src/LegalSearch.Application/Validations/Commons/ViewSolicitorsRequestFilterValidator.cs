using Fcmb.Shared.Models.Requests;
using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Commons
{
    public class ViewSolicitorsRequestFilterValidator : AbstractValidator<ViewSolicitorsRequestFilter>
    {
        public ViewSolicitorsRequestFilterValidator()
        {
            RuleFor(request => request.RegionId)
                .NotEmpty().When(request => request.RegionId.HasValue).WithMessage("Region ID is required.");

            RuleFor(request => request.FirmId)
                .NotEmpty().When(request => request.FirmId.HasValue).WithMessage("Firm ID is required.");

            RuleFor(request => request.Status)
                .IsInEnum().When(request => request.Status.HasValue).WithMessage("Invalid status.");
        }
    }

    public class PaginatedRequestValidator : AbstractValidator<PaginatedRequest>
    {
        public PaginatedRequestValidator()
        {
            RuleFor(request => request.PageNumber)
                .NotEmpty().WithMessage("Specify a page number.")
                .GreaterThanOrEqualTo(1).WithMessage("Provide a valid page number.");

            RuleFor(request => request.PageSize)
                .NotEmpty().WithMessage("Specify a page size.")
                .InclusiveBetween(1, 100).WithMessage("Provide a valid page size.");
        }
    }
}
