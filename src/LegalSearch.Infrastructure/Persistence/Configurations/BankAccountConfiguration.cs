using LegalSearch.Domain.Entities.User.Solicitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LegalSearch.Infrastructure.Persistence.Configurations
{
    public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
    {
        public void Configure(EntityTypeBuilder<BankAccount> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasIndex(c => new{c.AccountNumber, c.BankId}, "Idx_OneUniqueAccountInBank")
                .IsUnique()
                .HasFilter($" \"{nameof(BankAccount.IsDeleted)}\" = false");
        }
    }
    
    public class BankConfiguration : IEntityTypeConfiguration<Bank>
    {
        public void Configure(EntityTypeBuilder<Bank> builder)
        {
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter($" \"{nameof(Bank.IsDeleted)}\" = false");
        }
    }
}
