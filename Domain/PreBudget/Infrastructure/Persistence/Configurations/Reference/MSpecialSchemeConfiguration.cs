namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MSpecialSchemeConfiguration : IEntityTypeConfiguration<MSpecialScheme>
{
    public void Configure(EntityTypeBuilder<MSpecialScheme> builder)
    {
        builder.ToTable("M_SpecialScheme");
        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId).HasColumnName("RowId");
        builder.Property(e => e.SplSchemeName).HasColumnName("spl_SchemeName");
        builder.Property(e => e.StmtNo).HasColumnName("StmtNo");
    }
}
