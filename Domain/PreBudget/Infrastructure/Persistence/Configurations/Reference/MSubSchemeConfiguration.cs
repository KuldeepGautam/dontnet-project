namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MSubSchemeConfiguration : IEntityTypeConfiguration<MSubScheme>
{
    public void Configure(EntityTypeBuilder<MSubScheme> builder)
    {
        builder.ToTable("M_SubScheme");
        builder.HasKey(e => e.SubSchemeId);
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeName).HasColumnName("SubSchemeName");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(e => e.PrevSubschemeId).HasColumnName("PrevSubschemeId");
        builder.Property(e => e.SubSchemeSrNo).HasColumnName("SubSchemeSrNo");
    }
}
