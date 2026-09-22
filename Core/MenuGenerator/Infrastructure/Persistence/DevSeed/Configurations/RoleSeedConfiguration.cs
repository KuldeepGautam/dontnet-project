namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Dev/test-seed-only configuration for Role - creates and seeds "M_Role"
/// in the standalone dev/test database (separate from the production, read-only legacy tables).
/// </summary>
public class RoleSeedConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("M_Role", "dbo");
        builder.HasKey(x => x.RoleId);
        builder.Property(x => x.RoleId).HasColumnName("RoleId").ValueGeneratedNever();
        builder.Property(x => x.RoleName).HasColumnName("RoleName");
        builder.Property(x => x.Active).HasColumnName("Active");

        builder.HasData(
            new Role { RoleId = 13, RoleName = "Administrator", Active = "Y" },
            new Role { RoleId = 18, RoleName = "Single Demand Users", Active = "Y" },
            new Role { RoleId = 19, RoleName = "MTEF Admin", Active = "Y" },
            new Role { RoleId = 21, RoleName = "Budget Division", Active = "N" },
            new Role { RoleId = 22, RoleName = "Admin DS", Active = "N" },
            new Role { RoleId = 23, RoleName = "ABO-DS-Director", Active = "Y" },
            new Role { RoleId = 24, RoleName = "Admin JS", Active = "N" },
            new Role { RoleId = 25, RoleName = "Section User", Active = "Y" },
            new Role { RoleId = 27, RoleName = "Hindi Section", Active = "Y" },
            new Role { RoleId = 28, RoleName = "Budget Statement", Active = "N" },
            new Role { RoleId = 29, RoleName = "AFS Receipt Budget", Active = "Y" },
            new Role { RoleId = 30, RoleName = "Super Admin", Active = "N" },
            new Role { RoleId = 32, RoleName = "Stmt panda", Active = "N" },
            new Role { RoleId = 36, RoleName = "Admin SBE", Active = "N" },
            new Role { RoleId = 38, RoleName = "Section SBE", Active = "N" },
            new Role { RoleId = 39, RoleName = "Single  Demand Hindi User", Active = "N" },
            new Role { RoleId = 40, RoleName = "test test", Active = "N" },
            new Role { RoleId = 41, RoleName = "vivek test test test", Active = "N" },
            new Role { RoleId = 42, RoleName = "Report only", Active = "N" },
            new Role { RoleId = 43, RoleName = "Budget Entry", Active = "N" },
            new Role { RoleId = 44, RoleName = "BAG Budget", Active = "Y" },
            new Role { RoleId = 45, RoleName = "VOA Budget", Active = "Y" },
            new Role { RoleId = 46, RoleName = "NTR Budget", Active = "Y" },
            new Role { RoleId = 47, RoleName = "PAO NTR Budget", Active = "Y" },
            new Role { RoleId = 48, RoleName = "Analytics", Active = "N" },
            new Role { RoleId = 49, RoleName = "Receipt Budget", Active = "Y" },
            new Role { RoleId = 50, RoleName = "Debt Budget", Active = "Y" },
            new Role { RoleId = 51, RoleName = "NSSection", Active = "Y" },
            new Role { RoleId = 52, RoleName = "Ministry DebtModule", Active = "Y" },
            new Role { RoleId = 53, RoleName = "CAAA Debt", Active = "N" },
            new Role { RoleId = 54, RoleName = "ECL Expenditure", Active = "Y" },
            new Role { RoleId = 55, RoleName = "ECL NIC", Active = "Y" },
            new Role { RoleId = 56, RoleName = "State Devolution", Active = "Y" }
        );
    }
}
