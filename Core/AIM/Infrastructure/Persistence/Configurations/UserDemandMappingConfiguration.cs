namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class UserDemandMappingConfiguration : IEntityTypeConfiguration<UserDemandMapping>
{
    public void Configure(EntityTypeBuilder<UserDemandMapping> builder)
    {
        builder.ToTable("M_MapUserDemand", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(m => m.RowId);
        builder.Property(m => m.RowId).HasColumnName("RowId").ValueGeneratedOnAdd();
        builder.Property(m => m.UserId).HasColumnName("UserId");
        builder.Property(m => m.DemandIds).HasColumnName("DemandId").HasMaxLength(1000);
        builder.Property(m => m.PrevRowId).HasColumnName("PrevRowId");
        builder.Property(m => m.IsActive).HasColumnName("IsActive");
        builder.Property(m => m.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(m => m.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(m => m.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(m => m.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(m => m.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(m => m.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasIndex(m => m.UserId).HasDatabaseName("IX_M_MapUserDemand_UserId");
    }
}
