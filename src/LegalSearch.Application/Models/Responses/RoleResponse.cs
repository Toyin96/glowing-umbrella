namespace LegalSearch.Application.Models.Responses
{
    public class RoleResponse
    {
        public Guid RoleId { get; init; }

        public string RoleName { get; init; }

        public List<string> Permissions { get; init; }
    }
}
