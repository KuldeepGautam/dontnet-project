namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class AppendixViibTransactionTypeConfiguration : IEntityTypeConfiguration<AppendixViibTransactionType>
{
    public void Configure(EntityTypeBuilder<AppendixViibTransactionType> builder)
    {
        builder.ToTable("Appendix_VIIB_Transaction_Type");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id");
        builder.Property(e => e.Name).HasColumnName("Name");
    }
}
