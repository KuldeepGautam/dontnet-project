namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeDemandConfiguration : IEntityTypeConfiguration<SbeDemand>
{
    public void Configure(EntityTypeBuilder<SbeDemand> builder)
    {
        builder.ToTable("M_SBEDemand", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SbeDemandId);
        builder.Property(e => e.SbeDemandId).HasColumnName("SBEDemandId").ValueGeneratedOnAdd();
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.UserId).HasColumnName("UserId");
        builder.Property(e => e.Status).HasColumnName("Status").HasMaxLength(1);
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.TargetDate).HasColumnName("TargetDate");
        builder.Property(e => e.UnFreezeDate).HasColumnName("UnFreezeDate");
        builder.Property(e => e.Freez).HasColumnName("Freez").HasMaxLength(1);
        builder.Property(e => e.SbeFreezDate).HasColumnName("SBEFreezDate");
        builder.Property(e => e.FreezUserId).HasColumnName("FreezUserId");
        builder.Property(e => e.UnFreezeUserId).HasColumnName("UnFreezeUserid");
        builder.Property(e => e.ActualEditFlag).HasColumnName("Actual_Edit_Flag").HasMaxLength(1);
        builder.Property(e => e.BeEditFlag).HasColumnName("BE_Edit_Flag").HasMaxLength(1);
        builder.Property(e => e.ActualEditFlagDdg).HasColumnName("Actual_edit_flag_ddg").HasMaxLength(1);
        builder.Property(e => e.EditTargetDateDdg).HasColumnName("Edit_targetDate_ddg");
        builder.Property(e => e.SchemeCatEditFlag).HasColumnName("Scheme_Cat_Edit_Flag").HasMaxLength(1);
        builder.Property(e => e.AllowNegExpFlag).HasColumnName("Allow_NegExp_Flag").HasMaxLength(1);
        builder.Property(e => e.AllowNegExpFlagDdg).HasColumnName("Allow_NegExp_Flag_ddg").HasMaxLength(1);
        builder.Property(e => e.AllowNegRecFlag).HasColumnName("Allow_NegRec_Flag").HasMaxLength(1);
        builder.Property(e => e.EditTargetDate).HasColumnName("Edit_TargetDate");
        builder.Property(e => e.DdgTargetDate).HasColumnName("DDGTargetDate");
        builder.Property(e => e.DdgFreez).HasColumnName("DDGFreez").HasMaxLength(1);
        builder.Property(e => e.DdgFreezDate).HasColumnName("DDGFreezDate");
        builder.Property(e => e.ConsumedStatus).HasColumnName("ConsumedStatus").HasMaxLength(1);
        builder.Property(e => e.DdgUnFreezeDate).HasColumnName("DDGUnFreezeDate");
        builder.Property(e => e.DdgFreezUserId).HasColumnName("DDGFreezUserId");
        builder.Property(e => e.DdgUnFreezeUserId).HasColumnName("DDGUnFreezeUserId");

        builder.Ignore(e => e.IsFrozen);
        builder.HasIndex(e => new { e.DemandId, e.FinancialYear }).IsUnique();
    }
}
