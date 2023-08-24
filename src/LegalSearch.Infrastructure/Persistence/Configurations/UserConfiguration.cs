using LegalSearch.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasIndex(c => c.Id);

            builder.Property(x => x.LastName).IsRequired(false);

            // Configure FirmId as an optional property
            builder.Property(u => u.FirmId).IsRequired(false);

            // Configure the relationship with the Firm entity as optional
            builder.HasOne(u => u.Firm)
                .WithMany(f => f.Users)
                .HasForeignKey(u => u.FirmId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
