namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

/// <summary>Explicit Fluent API mapping for every property against dbo.SBEData's real columns (confirmed 2026-08-25 via sys.columns).</summary>
public class SbeDataConfiguration : IEntityTypeConfiguration<SbeData>
{
    public void Configure(EntityTypeBuilder<SbeData> builder)
    {
        builder.ToTable("SBEData", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SbeDataId);
        builder.Property(e => e.SbeDataId).HasColumnName("SBEDataID").ValueGeneratedOnAdd();

        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(50);
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.MinistryId).HasColumnName("MinistryId");
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryId");
        builder.Property(e => e.SubCategoryRptSrNo).HasColumnName("SubCategoryRptSrNo").HasMaxLength(4);
        builder.Property(e => e.UmbSchemeId).HasColumnName("UmbSchemeId");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeID");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeID");
        builder.Property(e => e.MajorHeadCode).HasColumnName("MajorHeadCode").HasMaxLength(4);

        builder.Property(e => e.ExpType).HasColumnName("Exp_Type").HasMaxLength(10);
        builder.Property(e => e.PlanType).HasColumnName("Plan_Type").HasMaxLength(1);

        builder.Property(e => e.ActualPlan).HasColumnName("Actual_plan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ActualNonPlan).HasColumnName("Actual_nonplan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BePlan).HasColumnName("BE_Plan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.BeNonPlan).HasColumnName("BE_nonplan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.RePlan).HasColumnName("RE_Plan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReNonPlan).HasColumnName("RE_Nonplan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NbePlan).HasColumnName("NBE_PLan").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NbeNonPlan).HasColumnName("NBE_NonPlan").HasColumnType("decimal(18,2)");

        builder.Property(e => e.ReceiptRecoveryMajorHeadCode).HasColumnName("Receipt_recovery_MajorHeadCode").HasMaxLength(4);
        builder.Property(e => e.SpecialStatements).HasColumnName("Special_Statements").HasMaxLength(25);
        builder.Property(e => e.SplSchemeId).HasColumnName("Spl_SchemeId");
        builder.Property(e => e.SplSchemeName).HasColumnName("Spl_SchemeName").HasMaxLength(250);
        builder.Property(e => e.HSplSchemeName).HasColumnName("hSpl_SchemeName").HasMaxLength(1000);
        builder.Property(e => e.SubsidyGroupId).HasColumnName("SubsidyGroupId");
        builder.Property(e => e.SubsidyGroup).HasColumnName("SubsidyGroup").HasMaxLength(20);
        builder.Property(e => e.HSubsidyGroup).HasColumnName("hSubsidyGroup").HasMaxLength(1000);
        builder.Property(e => e.SplSeqNo).HasColumnName("Spl_seqNo");
        builder.Property(e => e.AnnexIIGroupName).HasColumnName("AnnexII_GroupName").HasMaxLength(250);
        builder.Property(e => e.HAnnexIIGroupName).HasColumnName("HAnnexII_GroupName").HasMaxLength(1000);

        builder.Property(e => e.SbeDataIdPreviousYear).HasColumnName("SBEDataIDPreviousYear");
        builder.Property(e => e.SbeDetailCode).HasColumnName("SBEDetailCode").HasMaxLength(30);
        builder.Property(e => e.Active).HasColumnName("Active").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.LoginId).HasColumnName("LoginId");
        builder.Property(e => e.Stmt18Flag).HasColumnName("stmt18_flag").HasMaxLength(5);
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(20);

        builder.HasIndex(e => new { e.DemandId, e.FinancialYear, e.SchemeId });
    }
}
