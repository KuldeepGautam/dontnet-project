namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations.Reference;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities.Reference;

public class MSchemeConfiguration : IEntityTypeConfiguration<MScheme>
{
    public void Configure(EntityTypeBuilder<MScheme> builder)
    {
        builder.ToTable("M_Scheme");
        builder.HasKey(e => e.SchemeId);
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SchemeSrNo).HasColumnName("SchemeSrNo");
        builder.Property(e => e.SchemeName).HasColumnName("SchemeName");
        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(e => e.PrevSchemeId).HasColumnName("PrevSchemeId");
    }
}
