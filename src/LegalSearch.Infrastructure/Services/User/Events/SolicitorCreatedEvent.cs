using LegalSearch.Domain.Entities.User.Solicitor;
using MediatR;

namespace LegalSearch.Infrastructure.Services.User.Events
{
    public record SolicitorCreatedEvent(Solicitor User, string DefaultPassword) : INotification;
}
