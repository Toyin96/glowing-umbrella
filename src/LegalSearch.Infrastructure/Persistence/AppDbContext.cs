using System;
using System.Reflection;
using System.Threading.Tasks;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Domain.Entities.AuditLog;
using LegalSearch.Domain.Entities.Role;
using LegalSearch.Domain.Entities.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalSearch.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {
        private readonly DbContextOptions<AppDbContext> dco;

        public AppDbContext(DbContextOptions<AppDbContext> dco): base(dco)
        {
            this.dco = dco;
        }
        
        public DbSet<AuditLog> AuditLogs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            
            base.OnModelCreating(builder);
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (dco is not null)
            {
                base.OnConfiguring(optionsBuilder);
                return;
            }
            optionsBuilder.UseNpgsql(AppConstants.DbConnectionString, o => o.MigrationsAssembly("LegalSearch.Infrastructure"));
        }
        
        public async Task<bool> TrySaveChangesAsync(ILogger logger)
        {
            try
            {
                var result = await SaveChangesAsync();

                logger.LogInformation("Operation Affected {Result} items in Database", result);
                return true;
            }
            catch (DbUpdateException e)
            {
                logger.LogCritical(e, "An Error Occurred Saving Items");
            }

            return false;
        }
    }
}
