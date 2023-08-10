using LegalSearch.Domain.Entities.LegalRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    internal class LegalRequestConfiguration : IEntityTypeConfiguration<LegalRequest>
    {
        public void Configure(EntityTypeBuilder<LegalRequest> builder)
        {
            builder.HasMany(x => x.SupportingDocuments)
                .WithOne(x => x.LegalRequest)
                .IsRequired(false);
        }
    }
}
