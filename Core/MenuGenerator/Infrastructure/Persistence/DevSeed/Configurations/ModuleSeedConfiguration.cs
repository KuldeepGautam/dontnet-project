namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Dev/test-seed-only configuration for Module - creates and seeds "M_Module"
/// in the standalone dev/test database (separate from the production, read-only legacy tables).
/// </summary>
public class ModuleSeedConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("M_Module", "dbo");
        builder.HasKey(x => x.ModuleId);
        builder.Property(x => x.ModuleId).HasColumnName("ModuleId").ValueGeneratedNever();
        builder.Property(x => x.ModuleName).HasColumnName("ModuleName");
        builder.Property(x => x.Active).HasColumnName("Active");

        builder.HasData(
            new Module { ModuleId = 1, ModuleName = "Master Entry", Active = "Y" },
            new Module { ModuleId = 8, ModuleName = "Dashboard", Active = "Y" },
            new Module { ModuleId = 10, ModuleName = "Admin Screens", Active = "Y" },
            new Module { ModuleId = 22, ModuleName = "Audit Log", Active = "Y" },
            new Module { ModuleId = 26, ModuleName = "Logout", Active = "Y" },
            new Module { ModuleId = 27, ModuleName = "Change Password", Active = "Y" },
            new Module { ModuleId = 29, ModuleName = "Projections", Active = "Y" },
            new Module { ModuleId = 30, ModuleName = "Finalization", Active = "Y" },
            new Module { ModuleId = 31, ModuleName = "User", Active = "Y" },
            new Module { ModuleId = 32, ModuleName = "MTEF Reports", Active = "Y" },
            new Module { ModuleId = 34, ModuleName = "Master", Active = "Y" },
            new Module { ModuleId = 38, ModuleName = "Allocation", Active = "Y" },
            new Module { ModuleId = 39, ModuleName = "Update Contact", Active = "Y" },
            new Module { ModuleId = 40, ModuleName = "SBE Master", Active = "N" },
            new Module { ModuleId = 41, ModuleName = "Masters", Active = "Y" },
            new Module { ModuleId = 42, ModuleName = "Data Entry", Active = "Y" },
            new Module { ModuleId = 43, ModuleName = "Receipt Budget", Active = "Y" },
            new Module { ModuleId = 44, ModuleName = "SBE Notes", Active = "N" },
            new Module { ModuleId = 45, ModuleName = "Reports", Active = "Y" },
            new Module { ModuleId = 46, ModuleName = "Update Hindi Budget Master", Active = "Y" },
            new Module { ModuleId = 47, ModuleName = "Statement", Active = "Y" },
            new Module { ModuleId = 48, ModuleName = "Update Hindi  Receipt Budget", Active = "Y" },
            new Module { ModuleId = 49, ModuleName = "AFS", Active = "Y" },
            new Module { ModuleId = 50, ModuleName = "Glance", Active = "N" },
            new Module { ModuleId = 51, ModuleName = "MTEF Initialization", Active = "Y" },
            new Module { ModuleId = 52, ModuleName = "Suppbudget", Active = "Y" },
            new Module { ModuleId = 53, ModuleName = "test module", Active = "N" },
            new Module { ModuleId = 54, ModuleName = "Supp Reports", Active = "Y" },
            new Module { ModuleId = 55, ModuleName = "Supplementary Hindi Notes", Active = "Y" },
            new Module { ModuleId = 56, ModuleName = "DDG", Active = "Y" },
            new Module { ModuleId = 57, ModuleName = "Reports", Active = "Y" },
            new Module { ModuleId = 58, ModuleName = "UBIS Initialization", Active = "Y" },
            new Module { ModuleId = 59, ModuleName = "Initi Status", Active = "N" },
            new Module { ModuleId = 60, ModuleName = "RE Meeting", Active = "Y" },
            new Module { ModuleId = 61, ModuleName = "Supp Allocation", Active = "Y" },
            new Module { ModuleId = 62, ModuleName = "Update Hindi Statements", Active = "Y" },
            new Module { ModuleId = 63, ModuleName = "MTEF Allocation", Active = "Y" },
            new Module { ModuleId = 64, ModuleName = "Chart", Active = "N" },
            new Module { ModuleId = 65, ModuleName = "Update Hindi PA Receipt", Active = "Y" },
            new Module { ModuleId = 66, ModuleName = "Data Exchange", Active = "Y" },
            new Module { ModuleId = 67, ModuleName = "Master Initialization", Active = "Y" },
            new Module { ModuleId = 68, ModuleName = "Receipt Initi", Active = "Y" },
            new Module { ModuleId = 69, ModuleName = "Entry", Active = "Y" },
            new Module { ModuleId = 70, ModuleName = "VOA", Active = "Y" },
            new Module { ModuleId = 71, ModuleName = "BAG Report", Active = "Y" },
            new Module { ModuleId = 72, ModuleName = "BAG", Active = "Y" },
            new Module { ModuleId = 73, ModuleName = "SBE", Active = "Y" },
            new Module { ModuleId = 74, ModuleName = "Undertaking", Active = "N" },
            new Module { ModuleId = 75, ModuleName = "NTR Data Entry", Active = "Y" },
            new Module { ModuleId = 76, ModuleName = "NTR Report", Active = "Y" },
            new Module { ModuleId = 77, ModuleName = "Intrim Initialization", Active = "Y" },
            new Module { ModuleId = 78, ModuleName = "MajorHead Wise Analytics", Active = "Y" },
            new Module { ModuleId = 79, ModuleName = "Analytics Report", Active = "Y" },
            new Module { ModuleId = 80, ModuleName = "R Statement", Active = "Y" },
            new Module { ModuleId = 81, ModuleName = "R Budget", Active = "Y" },
            new Module { ModuleId = 82, ModuleName = "R Master", Active = "Y" },
            new Module { ModuleId = 83, ModuleName = "Statement Reports", Active = "Y" },
            new Module { ModuleId = 84, ModuleName = "Adhoc Queries", Active = "Y" },
            new Module { ModuleId = 85, ModuleName = "Machine Readable", Active = "Y" },
            new Module { ModuleId = 86, ModuleName = "Railways", Active = "Y" },
            new Module { ModuleId = 87, ModuleName = "NS Masters", Active = "Y" },
            new Module { ModuleId = 88, ModuleName = "NS Reports", Active = "Y" },
            new Module { ModuleId = 89, ModuleName = "NS Entry", Active = "Y" },
            new Module { ModuleId = 90, ModuleName = "Debt Data Entry", Active = "Y" },
            new Module { ModuleId = 91, ModuleName = "External Data Entry", Active = "Y" },
            new Module { ModuleId = 92, ModuleName = "DDG  Masters", Active = "Y" },
            new Module { ModuleId = 93, ModuleName = "Annex", Active = "Y" },
            new Module { ModuleId = 94, ModuleName = "ECL Master", Active = "Y" },
            new Module { ModuleId = 95, ModuleName = "ECL Data Entry", Active = "Y" },
            new Module { ModuleId = 96, ModuleName = "ECL Approve", Active = "Y" },
            new Module { ModuleId = 97, ModuleName = "ECL Report", Active = "Y" },
            new Module { ModuleId = 98, ModuleName = "Data Visualization", Active = "Y" },
            new Module { ModuleId = 99, ModuleName = "DDG Allocation", Active = "Y" },
            new Module { ModuleId = 100, ModuleName = "Update NSSF Master", Active = "Y" },
            new Module { ModuleId = 101, ModuleName = "Reports AFS", Active = "Y" },
            new Module { ModuleId = 102, ModuleName = "Reports BAG", Active = "Y" },
            new Module { ModuleId = 103, ModuleName = "Reports DDG", Active = "Y" },
            new Module { ModuleId = 104, ModuleName = "Reports ECL", Active = "Y" },
            new Module { ModuleId = 105, ModuleName = "Reports RE", Active = "Y" },
            new Module { ModuleId = 106, ModuleName = "Reports Receipt", Active = "Y" },
            new Module { ModuleId = 107, ModuleName = "Reports Statement", Active = "Y" },
            new Module { ModuleId = 108, ModuleName = "Reports Supplementary", Active = "Y" },
            new Module { ModuleId = 109, ModuleName = "Reports VOA", Active = "Y" },
            new Module { ModuleId = 110, ModuleName = "Reports UBIS", Active = "Y" },
            new Module { ModuleId = 111, ModuleName = "CFI Approve", Active = "Y" }
        );
    }
}
