namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>
/// EF Core mapping for the new dbo.UserId_StmtId_Mapping table. Excluded from EF migrations like
/// every other `dbo`-schema table in this project — its DDL is owned by the DBA-run script in
/// `db-scripts/UserId_StmtId_Mapping.md`, not by `dotnet ef database update`. Added 2026-07.
/// </summary>
public class UserStmtMappingConfiguration : IEntityTypeConfiguration<UserStmtMapping>
{
    public void Configure(EntityTypeBuilder<UserStmtMapping> builder)
    {
        builder.ToTable("UserId_StmtId_Mapping", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(m => m.MappingId);
        builder.Property(m => m.MappingId).HasColumnName("MappingId").ValueGeneratedOnAdd();
        builder.Property(m => m.UserId).HasColumnName("UserId");
        builder.Property(m => m.StmtId).HasColumnName("StmtId");
        builder.Property(m => m.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(m => m.Remarks).HasColumnName("Remarks").HasMaxLength(500);
        builder.Property(m => m.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(m => m.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(m => m.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(m => m.ModifiedOnDate).HasColumnName("ModifiedOnDate");

        builder.HasIndex(m => m.UserId).HasDatabaseName("IX_UserId_StmtId_Mapping_UserId");
        builder.HasIndex(m => m.StmtId).HasDatabaseName("IX_UserId_StmtId_Mapping_StmtId");
    }
}
