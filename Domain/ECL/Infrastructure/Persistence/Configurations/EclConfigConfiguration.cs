namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

public class EclConfigConfiguration : IEntityTypeConfiguration<EclConfig>
{
    public void Configure(EntityTypeBuilder<EclConfig> builder)
    {
        builder.ToTable("ECL_Config", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(e => e.EclStartYear).HasColumnName("ECL_StartYear").HasMaxLength(9).IsRequired();
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(e => e.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(e => e.ModifiedOnDate).HasColumnName("ModifiedOnDate");
    }
}
