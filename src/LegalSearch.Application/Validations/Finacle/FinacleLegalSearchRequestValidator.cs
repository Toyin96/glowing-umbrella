using FluentValidation;
using LegalSearch.Application.Models.Requests;

namespace LegalSearch.Application.Validations.Finacle
{
    public class FinacleLegalSearchRequestValidator : AbstractValidator<FinacleLegalSearchRequest>
    {
        public FinacleLegalSearchRequestValidator()
        {
            RuleFor(request => request.BranchId)
                .NotEmpty().WithMessage("Branch ID is required.");

            RuleFor(request => request.CustomerAccountName)
                .NotEmpty().WithMessage("Customer account name is required.");

            RuleFor(request => request.CustomerAccountNumber)
                .NotEmpty().WithMessage("Customer account number is required.");
        }
    }
}
