using FluentValidation;
using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Validations.Auth
{
    public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidator()
        {
            RuleFor(request => request.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(7).WithMessage("New password must be at least 7 characters long.")
                .Matches("[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("New password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("New password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("New password must contain at least one special character.");

            RuleFor(request => request.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirmation password is required.")
                .Equal(request => request.NewPassword).WithMessage("The password and confirmation password do not match.");

            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address.");

            RuleFor(request => request.Token)
                .NotEmpty().WithMessage("Token is required.");
        }
    }
}
