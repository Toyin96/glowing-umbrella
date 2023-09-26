using LegalSearch.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class ZonalServiceManagerConfiguration : IEntityTypeConfiguration<ZonalServiceManager>
    {
        public void Configure(EntityTypeBuilder<ZonalServiceManager> builder)
        {
            //builder.HasKey(x => x.Id);
            //builder.HasMany(x => x.Branches)
            //       .WithOne(x => x.ZonalServiceManager)
            //       .IsRequired(false)
            //       .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Branches)
                   .WithOne(x => x.ZonalServiceManager)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
