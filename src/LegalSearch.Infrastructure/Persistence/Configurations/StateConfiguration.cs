using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class StateConfiguration : IEntityTypeConfiguration<State>
    {
        public void Configure(EntityTypeBuilder<State> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter($" \"{nameof(State.IsDeleted)}\" = false");
        }
    }
    public class LgaConfiguration : IEntityTypeConfiguration<Lga>
    {
        public void Configure(EntityTypeBuilder<Lga> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);
            
            builder.HasIndex(c => new {c.Name, c.StateId}, "Idx_OneUniqueLgaInState")
                .IsUnique()
                .HasFilter($" \"{nameof(Lga.IsDeleted)}\" = false");
        }
    }
}
