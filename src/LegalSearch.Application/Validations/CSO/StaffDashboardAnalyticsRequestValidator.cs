using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;

namespace LegalSearch.Application.Validations.CSO
{
    public class StaffDashboardAnalyticsRequestValidator : AbstractValidator<StaffDashboardAnalyticsRequest>
    {
        public StaffDashboardAnalyticsRequestValidator()
        {
            RuleFor(request => request.CsoRequestStatusType)
                .IsInEnum().When(request => request.CsoRequestStatusType.HasValue)
                .WithMessage("Invalid CSO request status type.");

            RuleFor(request => request.BranchId)
                .NotEmpty().When(request => string.IsNullOrEmpty(request.BranchId))
                .WithMessage("Branch ID is required.");

            RuleFor(request => request.BranchId)
                .MaximumLength(50).When(request => !string.IsNullOrEmpty(request.BranchId))
                .WithMessage("Branch ID exceeds maximum length of 50 characters.");
        }
    }
}
