﻿using LegalSearch.Domain.Entities.User.Solicitor;
using LegalSearch.Domain.Enums.User;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.User
{
    public class User : IdentityUser<Guid>
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public OnboardingStatusType OnboardingStatus { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? BankAccount { get; set; }
        public string? ManagerName { get; set; }
        public string? ManagerDepartment { get; set; }
        public string? Department { get; set; }
        public string? StaffId { get; set; }
        public string? BranchId { get; set; }
        public string? SolId { get; set; }
        public string? FullName => $"{FirstName} {LastName}";
        public string? UnlockCode { get; set; }
        public DateTime? UnlockCodeExpiration { get; set; }
        public string? ProfileStatus { get; set; }

        // configuring relationships
        [ForeignKey("Firm")]
        public Guid? FirmId { get; set; }
        public Firm? Firm { get; set; }
        [ForeignKey("State")]
        public Guid? StateId { get; set; }
        public State? State { get; set; }
        public Guid? RoleId { get; set; }
        public Role.Role Role { get; set; }
        public ICollection<LegalRequest.LegalRequest>? LegalRequests { get; set; } = new List<LegalRequest.LegalRequest>();
    }
}
