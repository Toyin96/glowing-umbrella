using LegalSearch.Domain.Entities.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasIndex(c => c.Name)
                .IsUnique();
        }
    }
}
