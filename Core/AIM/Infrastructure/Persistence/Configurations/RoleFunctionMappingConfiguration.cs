namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class RoleFunctionMappingConfiguration : IEntityTypeConfiguration<RoleFunctionMapping>
{
    public void Configure(EntityTypeBuilder<RoleFunctionMapping> builder)
    {
        builder.ToTable("M_MapRoleFunction", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(rf => rf.RFMappingId);
        builder.Property(rf => rf.RFMappingId).HasColumnName("RFMappingId").ValueGeneratedOnAdd();
        builder.Property(rf => rf.AppId).HasColumnName("AppId");
        builder.Property(rf => rf.RoleId).HasColumnName("RoleId");
        builder.Property(rf => rf.ModuleId).HasColumnName("ModuleId");
        builder.Property(rf => rf.FunctionId).HasColumnName("FunctionId");
        builder.Property(rf => rf.Active).HasColumnName("Active");
        builder.Property(rf => rf.RFSequenceNo).HasColumnName("RFSequenceNo");
        builder.Property(rf => rf.Remarks).HasColumnName("Remarks");
        builder.Property(rf => rf.RFFreez).HasColumnName("RFFreez");
        builder.Property(rf => rf.UserRFFreez).HasColumnName("UserRFFreez");

        builder.HasIndex(rf => rf.RoleId).HasDatabaseName("IX_M_MapRoleFunction_RoleId");
        builder.HasIndex(rf => rf.FunctionId).HasDatabaseName("IX_M_MapRoleFunction_FunctionId");
    }
}
