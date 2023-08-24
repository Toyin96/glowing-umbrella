using LegalSearch.Domain.Enums.Role;

namespace LegalSearch.Application.Models.Requests.User
{
    public record OnboardNewUserRequest : BaseUserRequest
    {
        public RoleType UserRole { get; set; }
    }
}
