namespace LegalSearch.Domain.Entities.User.LegalPerfectionTeam
{
    public class LegalPerfectionTeam : User
    {
        // Role properties
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
    }
}
