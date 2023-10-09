using Fcmb.Shared.Models.Constants;
using FluentValidation;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Validations.Auth
{
    public class OnboardNewUserRequestValidator : AbstractValidator<OnboardNewUserRequest>
    {
        public OnboardNewUserRequestValidator()
        {
            Include(new BaseUserRequestValidator());

            RuleFor(request => request.RoleId)
                .NotEmpty().WithMessage("Role ID is required.")
                .NotEqual(Guid.Empty).WithMessage("Role ID must be a valid GUID.");
        }
    }

    public class BaseUserRequestValidator : AbstractValidator<BaseUserRequest>
    {
        public BaseUserRequestValidator()
        {
            RuleFor(request => request.FirstName)
                .NotEmpty().WithMessage("Please provide first name.")
                .Matches(RegexConstants.FullNameRegex).WithMessage("Please provide a valid first name.")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

            RuleFor(request => request.LastName)
                .NotEmpty().WithMessage("Please provide last name.")
                .Matches(RegexConstants.FullNameRegex).WithMessage("Please provide a valid last name.")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

            RuleFor(request => request.PhoneNumber)
                .NotEmpty().WithMessage("Please provide phone number.")
                .Matches(RegexConstants.PhoneNumberRegex).WithMessage("Please provide a valid phone number.")
                .MaximumLength(14).WithMessage("Phone number cannot exceed 14 characters.");

            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("Please provide email.")
                .Matches(RegexConstants.EmailRegex).WithMessage("Please provide a valid email address.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");
        }
    }
}
