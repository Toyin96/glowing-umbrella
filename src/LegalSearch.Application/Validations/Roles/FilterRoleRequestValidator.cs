using Fcmb.Shared.Models.Constants;
using FluentValidation;
using LegalSearch.Application.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalSearch.Application.Validations.Roles
{
    public class FilterRoleRequestValidator : AbstractValidator<FilterRoleRequest>
    {
        public FilterRoleRequestValidator()
        {
            RuleFor(request => request.RoleName)
                .Matches(RegexConstants.TextRegex).When(request => !string.IsNullOrEmpty(request.RoleName))
                .WithMessage("Invalid role name format.");

            RuleFor(request => request.Permissions)
                .NotNull().WithMessage("Permissions list cannot be null.");
        }
    }
}
