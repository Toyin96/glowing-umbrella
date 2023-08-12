using LegalSearch.Domain.Entities.AuditLog;
using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Entities.Location;
using LegalSearch.Domain.Entities.Notification;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace LegalSearch.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> dco) : base(dco) { }

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<User> Solicitors { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<LegalRequest> LegalSearchRequests { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Discussion> Discussions { get; set; }
        public DbSet<SolicitorAssignment> SolicitorAssignments { get; set; }
        public DbSet<Firm> Firms { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<User>()
                .HasOne(s => s.Firm)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
