namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

public class RoleFunctionMappingConfiguration : IEntityTypeConfiguration<RoleFunctionMapping>
{
    public void Configure(EntityTypeBuilder<RoleFunctionMapping> builder)
    {
        builder.ToTable("M_MapRoleFunction", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(rf => rf.RfMappingId);
        builder.Property(rf => rf.RfMappingId).HasColumnName("RFMappingId").ValueGeneratedOnAdd();
        builder.Property(rf => rf.AppId).HasColumnName("AppId");
        builder.Property(rf => rf.RoleId).HasColumnName("RoleId");
        // Real column is "ModuleId" (confirmed against the DBA's new-tables/M_RoleFunctionMapping.txt
        // export 2026-07-13), not "ModuleID".
        builder.Property(rf => rf.ModuleId).HasColumnName("ModuleId");
        builder.Property(rf => rf.FunctionId).HasColumnName("FunctionId");
        builder.Property(rf => rf.Active).HasColumnName("Active");
        builder.Property(rf => rf.RfSequenceNo).HasColumnName("RFSequenceNo");
        builder.Property(rf => rf.RFFreez).HasColumnName("RFFreez");
        builder.Property(rf => rf.UserRFFreez).HasColumnName("UserRFFreez");
    }
}
