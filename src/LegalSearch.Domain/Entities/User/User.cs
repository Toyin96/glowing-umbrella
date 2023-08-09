using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Identity;

namespace LegalSearch.Domain.Entities.User
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Firm Firm { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerDepartment { get; set; }
        public string? Department { get; set; }
        public string? StaffId { get; init; }
        public string? BranchId { get; set; }
        public string? SolId { get; set; }

        // Role properties
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
        public ICollection<LegalRequest.LegalRequest> LegalRequests { get; set; } = new List<LegalRequest.LegalRequest>();

        public string FullName => $"{FirstName} {LastName}";
    }
}
