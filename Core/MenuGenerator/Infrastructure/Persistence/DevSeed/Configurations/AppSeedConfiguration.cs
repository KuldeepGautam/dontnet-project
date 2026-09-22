namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// Dev/test-seed-only configuration for App - creates and seeds "M_App" in the standalone
/// dev/test database. Same 14 rows as the production AppConfiguration seed.
/// </summary>
public class AppSeedConfiguration : IEntityTypeConfiguration<App>
{
    public void Configure(EntityTypeBuilder<App> builder)
    {
        builder.ToTable("M_App", "dbo");
        builder.HasKey(a => a.AppId);
        builder.Property(a => a.AppId).HasColumnName("AppId").ValueGeneratedNever();
        builder.Property(a => a.AppName).HasColumnName("AppName").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Active).HasColumnName("Active");
        builder.Property(a => a.PrintSeq).HasColumnName("PrintSeq");

        builder.HasData(
            new App { AppId = 1, AppName = "Exp Budget(SBE)", Active = "Y", PrintSeq = 4 },
            new App { AppId = 2, AppName = "MTEF", Active = "Y", PrintSeq = 11 },
            new App { AppId = 3, AppName = "Supplementary Budget", Active = "Y", PrintSeq = 6 },
            new App { AppId = 4, AppName = "Combined Dashboard", Active = "Y", PrintSeq = 7 },
            new App { AppId = 5, AppName = "DDG", Active = "Y", PrintSeq = 2 },
            new App { AppId = 6, AppName = "Exp Profile", Active = "Y", PrintSeq = 5 },
            new App { AppId = 7, AppName = "RE Meeting", Active = "Y", PrintSeq = 1 },
            new App { AppId = 8, AppName = "Receipt Budget", Active = "Y", PrintSeq = 8 },
            new App { AppId = 9, AppName = "Debt Module", Active = "Y", PrintSeq = 9 },
            new App { AppId = 10, AppName = "NSModule", Active = "Y", PrintSeq = 10 },
            new App { AppId = 11, AppName = "ECL", Active = "Y", PrintSeq = 3 },
            new App { AppId = 12, AppName = "Reappropriation", Active = "Y", PrintSeq = 12 },
            new App { AppId = 13, AppName = "Autonomous/Grantee Bodies", Active = "Y", PrintSeq = 13 },
            new App { AppId = 14, AppName = "Contingency Advance", Active = "Y", PrintSeq = 14 });
    }
}
