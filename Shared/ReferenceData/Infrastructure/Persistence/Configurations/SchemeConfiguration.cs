namespace UBIS.Services.ReferenceData.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.ReferenceData.Domain.Entities;

public class SchemeConfiguration : IEntityTypeConfiguration<Scheme>
{
    public void Configure(EntityTypeBuilder<Scheme> builder)
    {
        builder.ToTable("M_Scheme", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(s => s.SchemeId);
        builder.Property(s => s.SchemeId).HasColumnName("SchemeId").ValueGeneratedOnAdd();
        builder.Property(s => s.DemandId).HasColumnName("DemandId");
        builder.Property(s => s.SchemeName).HasColumnName("SchemeName").HasMaxLength(250).IsRequired();
        builder.Property(s => s.HSchemeName).HasColumnName("HSchemeName").HasMaxLength(500);
        builder.Property(s => s.IsUmbrella).HasColumnName("IsUmbrella");
        builder.Property(s => s.IsActive).HasColumnName("IsActive");
        builder.Property(s => s.IsDeleted).HasColumnName("IsDeleted");
        builder.Property(s => s.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(s => s.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(s => s.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(s => s.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(s => s.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(s => s.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasQueryFilter(s => !s.IsDeleted);
        builder.HasIndex(s => s.DemandId).HasDatabaseName("IX_M_Scheme_DemandId");
    }
}
