using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class StateConfiguration : IEntityTypeConfiguration<State>
    {
        public void Configure(EntityTypeBuilder<State> builder)
        {
            // Set the primary key
            builder.HasKey(s => s.Id);

            // Set the name property as a unique index (if needed)
            builder.HasIndex(s => s.Name).IsUnique();

            // Set the foreign key relationship with the Region entity
            builder.HasOne(s => s.Region)
                   .WithMany(r => r.States)
                   .HasForeignKey(s => s.RegionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Add any additional configurations or constraints here
        }
    }
    public class LgaConfiguration : IEntityTypeConfiguration<Region>
    {
        public void Configure(EntityTypeBuilder<Region> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
