using Fcmb.Shared.Models.Constants;
using FluentValidation;
using LegalSearch.Application.Models.Requests;

namespace LegalSearch.Application.Validations.Roles
{
    public class RoleRequestValidator : AbstractValidator<RoleRequest>
    {
        public RoleRequestValidator()
        {
            RuleFor(request => request.RoleName)
                .NotEmpty().WithMessage("Role name is required.")
                .Matches(RegexConstants.TextRegex).WithMessage("Invalid role name format.")
                .MaximumLength(100).WithMessage("Role name exceeds maximum length of 100 characters.");

            RuleFor(request => request.Permissions)
                .NotNull().WithMessage("Permissions list cannot be null.");
        }
    }
}
