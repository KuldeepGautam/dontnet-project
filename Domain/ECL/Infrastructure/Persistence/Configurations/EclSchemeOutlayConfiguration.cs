namespace UBIS.Services.Ecl.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Ecl.Domain.Entities;

/// <summary>
/// Explicit Fluent API mapping for every property against dbo.ECL_T_Outlay's real columns
/// (confirmed 2026-08-18 via INFORMATION_SCHEMA.COLUMNS, re-confirmed after
/// ecl-workstream-4-outlay-column-cleanup.sql's renames/drops). DB-first, no EF migrations —
/// ExcludeFromMigrations() matches PreBudget's own convention even though this solution never runs
/// migrations against this table. FileTitle is deliberately left unmapped — see EclSchemeOutlay's
/// class doc comment.
/// </summary>
public class EclSchemeOutlayConfiguration : IEntityTypeConfiguration<EclSchemeOutlay>
{
    public void Configure(EntityTypeBuilder<EclSchemeOutlay> builder)
    {
        builder.ToTable("ECL_T_Outlay", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId).HasColumnName("RowID").ValueGeneratedOnAdd();

        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.CategoryId).HasColumnName("CategoryId");
        builder.Property(e => e.SubCategoryId).HasColumnName("SubCategoryId");
        builder.Property(e => e.DemandId).HasColumnName("DemandID");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeID");
        builder.Property(e => e.ApproveAuth).HasColumnName("ApproveAuth").HasMaxLength(100);
        builder.Property(e => e.ApprovalAuthorityId).HasColumnName("ApprovalAuthorityId");

        builder.Property(e => e.TotalOutlay).HasColumnName("TotalOutlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CentralSharePercentage).HasColumnName("Percentage").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CentralShare).HasColumnName("CentralShare").HasColumnType("decimal(18,2)");

        builder.Property(e => e.Fy1Outlay).HasColumnName("FY1_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy2Outlay).HasColumnName("FY2_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy3Outlay).HasColumnName("FY3_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy4Outlay).HasColumnName("FY4_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy5Outlay).HasColumnName("FY5_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy6Outlay).HasColumnName("FY6_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy7Outlay).HasColumnName("FY7_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy8Outlay).HasColumnName("FY8_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy9Outlay).HasColumnName("FY9_Outlay").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy10Outlay).HasColumnName("FY10_Outlay").HasColumnType("decimal(18,2)");

        builder.Property(e => e.FileName).HasColumnName("FileName").HasMaxLength(250);
        builder.Property(e => e.UserRemarks).HasColumnName("UserRemarks");
        builder.Property(e => e.EntryUserId).HasColumnName("EntryUserID");
        builder.Property(e => e.SendToDoe).HasColumnName("SendToDOE").HasMaxLength(1);
        builder.Property(e => e.EntryDateDemand).HasColumnName("EntryDateDemand");
        builder.Property(e => e.IpDemand).HasColumnName("IPDemand").HasMaxLength(16);

        builder.Property(e => e.ApprovedByDoe).HasColumnName("ApproveByDOE").HasMaxLength(1);
        builder.Property(e => e.ReapprovalByDoe).HasColumnName("ReapproveByDOE").HasMaxLength(1);
        builder.Property(e => e.ApproveUserId).HasColumnName("ApproveUserId");
        builder.Property(e => e.ApproveDate).HasColumnName("ApproveDate");
        builder.Property(e => e.ApproverIp).HasColumnName("ApproverIP").HasMaxLength(16);
        builder.Property(e => e.DoeRemarks).HasColumnName("Remarks");

        builder.Property(e => e.Fy1Actuals).HasColumnName("FY1_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy2Actuals).HasColumnName("FY2_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy3Actuals).HasColumnName("FY3_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy4Actuals).HasColumnName("FY4_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy5Actuals).HasColumnName("FY5_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy6Actuals).HasColumnName("FY6_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy7Actuals).HasColumnName("FY7_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy8Actuals).HasColumnName("FY8_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy9Actuals).HasColumnName("FY9_Actuals").HasColumnType("decimal(18,2)");
        builder.Property(e => e.Fy10Actuals).HasColumnName("FY10_Actuals").HasColumnType("decimal(18,2)");

        builder.Property(e => e.ActualsUserId).HasColumnName("ActualsUserId");
        builder.Property(e => e.ActualsIp).HasColumnName("ActualsIP").HasMaxLength(16);
        builder.Property(e => e.ActualsEntryDate).HasColumnName("ActualsEntryDate");

        builder.Property(e => e.PrevRowId).HasColumnName("PrevRowId");
        builder.Property(e => e.Is16Fc).HasColumnName("Is16FC").HasMaxLength(1);
        builder.Property(e => e.AppraisalAuth).HasColumnName("AppraisalAuth").HasMaxLength(100);
        builder.Property(e => e.AppraisalAuthorityId).HasColumnName("AppraisalAuthorityId");
        builder.Property(e => e.WhetherAppraised).HasColumnName("IsApprised").HasMaxLength(1);
        builder.Property(e => e.AppraisalStatusRemarks).HasColumnName("NotApprisedRem").HasMaxLength(500);
        builder.Property(e => e.IsApproved).HasColumnName("IsApproved").HasMaxLength(1);
        builder.Property(e => e.NotApprovedRem).HasColumnName("NotApprovedRem").HasMaxLength(500);

        builder.Property(e => e.IsActive).HasColumnName("IsActive");
        builder.Property(e => e.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(e => e.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(e => e.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(e => e.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(e => e.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(e => e.DeletedOnDate).HasColumnName("DeletedOnDate");
        builder.Property(e => e.IsDeleted).HasColumnName("IsDeleted").IsRequired();
        builder.Property(e => e.DeletedByUserId).HasColumnName("DeletedByUserId");
        builder.Property(e => e.DeletedByIp).HasColumnName("DeletedByIP").HasMaxLength(50);

        builder.Property(e => e.SchemeEndYear).HasColumnName("SchemeEndYear").HasMaxLength(9);

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => new { e.DemandId, e.CategoryId, e.SchemeId, e.FinancialYear });
    }
}
