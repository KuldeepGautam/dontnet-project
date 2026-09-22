namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

public class RoleModuleMappingConfiguration : IEntityTypeConfiguration<RoleModuleMapping>
{
    public void Configure(EntityTypeBuilder<RoleModuleMapping> builder)
    {
        builder.ToTable("M_MapRoleModule", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(rm => rm.RModuleMappingId);
        // Real column is "RMMappingId" (confirmed against the DBA's new-tables/M_RoleModuleMapping.txt
        // export 2026-07-13) — the entity property name doesn't have to match the column name.
        builder.Property(rm => rm.RModuleMappingId).HasColumnName("RMMappingId").ValueGeneratedOnAdd();
        builder.Property(rm => rm.AppId).HasColumnName("AppId");
        builder.Property(rm => rm.RoleId).HasColumnName("RoleId");
        builder.Property(rm => rm.ModuleId).HasColumnName("ModuleId");
        builder.Property(rm => rm.Active).HasColumnName("Active");
        builder.Property(rm => rm.RmSequenceNo).HasColumnName("RMSequenceNo");
        builder.Property(rm => rm.RMFreez).HasColumnName("RMFreez");
        builder.Property(rm => rm.UserMFreez).HasColumnName("UserMFreez");
    }
}
