using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class SolicitorConfiguration : IEntityTypeConfiguration<Solicitor>
    {
        public void Configure(EntityTypeBuilder<Solicitor> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasIndex(c => new { c.FirstName, c.LastName })
                .HasFilter($" \"{nameof(Solicitor.IsDeleted)}\" = false");
        }
    }
}
