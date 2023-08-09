namespace LegalSearch.Domain.Entities.User.CustomerServiceOfficer
{
    public class CustomerServiceOfficer : User
    {
        // Role properties
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
    }
}
