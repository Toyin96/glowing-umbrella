using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    internal class SolicitorConfiguration : IEntityTypeConfiguration<Domain.Entities.User.User>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.User.User> builder)
        {
            //builder.HasOne(u => u.State)
            //.WithMany()
            //.HasForeignKey(u => u.StateId)
            //.OnDelete(DeleteBehavior.Restrict);
        }
    }
}
