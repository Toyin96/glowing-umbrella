namespace LegalSearch.Application.Models.Requests.User
{
    public record OnboardNewUserRequest : BaseUserRequest
    {
        public Guid RoleId { get; set; }
    }
}
