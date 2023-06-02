using System;
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
        
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        

        public string FullName => $"{FirstName} {LastName}";
    }
}
