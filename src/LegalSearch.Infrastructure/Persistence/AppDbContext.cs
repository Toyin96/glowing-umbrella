using System.Reflection;
using LegalSearch.Domain.Entities.AuditLog;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LegalSearch.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> dco): base(dco){}
        
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Solicitor> Solicitors { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Firm> Firms { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Solicitor>()
                .HasOne(s => s.Firm)
                .WithMany()
                .HasForeignKey(s => s.FirmId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitor>()
                .HasOne(s => s.State)
                .WithMany()
                .HasForeignKey(s => s.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitor>()
                .HasOne(s => s.Region)
                .WithMany()
                .HasForeignKey(s => s.RegionId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
