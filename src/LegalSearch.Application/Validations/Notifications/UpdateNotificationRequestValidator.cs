using FluentValidation;
using LegalSearch.Application.Models.Requests.Notification;

namespace LegalSearch.Application.Validations.Notifications
{
    public class UpdateNotificationRequestValidator : AbstractValidator<UpdateNotificationRequest>
    {
        public UpdateNotificationRequestValidator()
        {
            RuleFor(request => request.NotificationId)
                .NotEmpty().WithMessage("Notification ID is required.");
        }
    }
}
