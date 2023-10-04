using FluentValidation;
using LegalSearch.Application.Models.Requests.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
