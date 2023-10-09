using LegalSearch.Domain.Enums.User;

namespace LegalSearch.Application.Models.Requests.Solicitor
{
    public class ActivateOrDeactivateSolicitorRequest
    {
        public Guid SolicitorId { get; set; }
        public ProfileStatusActionType ActionType { get; set; }
    }
}
