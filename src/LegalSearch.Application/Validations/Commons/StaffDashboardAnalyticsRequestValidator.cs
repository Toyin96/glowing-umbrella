using FluentValidation;
using LegalSearch.Application.Models.Requests.CSO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Commons
{
    public class StaffDashboardAnalyticsRequestValidator : AbstractValidator<StaffDashboardAnalyticsRequest>
    {
        public StaffDashboardAnalyticsRequestValidator()
        {
            RuleFor(request => request.CsoRequestStatusType)
                .IsInEnum().WithMessage("Invalid CSO request status type.");

            RuleFor(request => request.StartPeriod)
                .Must(BeAValidDate).When(request => request.StartPeriod.HasValue).WithMessage("Invalid start period date.");

            RuleFor(request => request.EndPeriod)
                .Must(BeAValidDate).When(request => request.EndPeriod.HasValue).WithMessage("Invalid end period date.")
                .GreaterThanOrEqualTo(request => request.StartPeriod).When(request => request.StartPeriod.HasValue && request.EndPeriod.HasValue)
                .WithMessage("End period should be greater than or equal to start period.");
        }

        private bool BeAValidDate(DateTime? date)
        {
            return !date.HasValue || date <= DateTime.UtcNow.AddHours(1);
        }
    }
}
