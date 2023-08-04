using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class FirmConfiguration : IEntityTypeConfiguration<Firm>
    {
        public void Configure(EntityTypeBuilder<Firm> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasIndex(c => c.Name)
                .IsUnique(false);
        }
    }
}
