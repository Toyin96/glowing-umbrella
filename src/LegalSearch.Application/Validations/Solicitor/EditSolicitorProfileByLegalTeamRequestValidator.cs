using FluentValidation;
using LegalSearch.Application.Models.Requests.Solicitor;

namespace LegalSearch.Application.Validations.Solicitor
{
    public class EditSolicitorProfileByLegalTeamRequestValidator : AbstractValidator<EditSolicitorProfileByLegalTeamRequest>
    {
        public EditSolicitorProfileByLegalTeamRequestValidator()
        {
            RuleFor(request => request.SolicitorId)
                .NotEmpty().WithMessage("Solicitor ID is required.");

            RuleFor(request => request.FirstName)
                .NotEmpty().WithMessage("First name is required.");

            RuleFor(request => request.LastName)
                .NotEmpty().WithMessage("Last name is required.");

            RuleFor(request => request.FirmName)
                .NotEmpty().WithMessage("Firm name is required.");

            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address.");

            RuleFor(request => request.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.");

            RuleFor(request => request.State)
                .NotEmpty().WithMessage("State is required.");

            RuleFor(request => request.Address)
                .NotEmpty().WithMessage("Address is required.");

            RuleFor(request => request.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.");
        }
    }
}
