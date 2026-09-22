namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

/// <summary>
/// M_AppName is a pre-existing legacy table (read-only), same as M_Role/M_Module/M_Function.
/// Corrected 2026-07-21: a prior pass (2026-07-16) mapped this to "App_Name", taking that name
/// from UBIS_RBAC.xlsx's sheet *tab label* rather than the real SQL object name — that table
/// never existed in BIMS2 at all, breaking every menu fetch. Confirmed against the client-provided
/// BIMSDemo database (real legacy data, same 14 rows as the xlsx export): the actual table is
/// dbo.M_AppName. Like every other table in this solution, no EF migrations —
/// `ExcludeFromMigrations()`.
/// </summary>
public class AppConfiguration : IEntityTypeConfiguration<App>
{
    public void Configure(EntityTypeBuilder<App> builder)
    {
        builder.ToTable("M_AppName", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(a => a.AppId);
        builder.Property(a => a.AppId).HasColumnName("AppId").ValueGeneratedNever();
        builder.Property(a => a.AppName).HasColumnName("AppName").HasMaxLength(200).IsRequired();
        builder.Property(a => a.Active).HasColumnName("Active");
        builder.Property(a => a.PrintSeq).HasColumnName("PrintSeq");
        builder.Property(a => a.AreaSlug).HasColumnName("AreaSlug").HasMaxLength(200);
    }
}
