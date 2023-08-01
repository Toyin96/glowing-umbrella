using Microsoft.AspNetCore.Identity;

namespace LegalSearch.Domain.Entities.Role
{
    public class Role : IdentityRole<Guid>
    {
        public Role()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public List<RolePermission> Permissions { get; set; } = new();
        
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
