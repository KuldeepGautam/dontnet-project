namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>EF Core mapping for the legacy dbo.M_SBEDemand pass-through. Added 2026-07-10.</summary>
public class SBEDemandConfiguration : IEntityTypeConfiguration<SBEDemand>
{
    public void Configure(EntityTypeBuilder<SBEDemand> builder)
    {
        builder.ToTable("M_SBEDemand", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(d => d.SBEDemandId);
        builder.Property(d => d.SBEDemandId).HasColumnName("SBEDemandId").ValueGeneratedNever();
        builder.Property(d => d.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(d => d.DemandId).HasColumnName("DemandId");
        builder.Property(d => d.UserId).HasColumnName("UserId");
        builder.Property(d => d.Status).HasColumnName("Status");
        builder.Property(d => d.EntryDate).HasColumnName("EntryDate");
        builder.Property(d => d.TargetDate).HasColumnName("TargetDate");
        builder.Property(d => d.UnFreezedDate).HasColumnName("UnFreezedDate");
        builder.Property(d => d.Freez).HasColumnName("Freez");
        builder.Property(d => d.SBEFreezDate).HasColumnName("SBEFreezDate");
        builder.Property(d => d.FreezUserId).HasColumnName("FreezUserId");
        builder.Property(d => d.UnFreezeUserId).HasColumnName("UnFreezeUserId");
        builder.Property(d => d.Actual_Edit_Flag).HasColumnName("Actual_Edit_Flag");
        builder.Property(d => d.BE_Edit_Flag).HasColumnName("BE_Edit_Flag");
        builder.Property(d => d.Actual_edit_flag_ddg).HasColumnName("Actual_edit_flag_ddg");
        builder.Property(d => d.Edit_target_date_ddg).HasColumnName("Edit_target_date_ddg");
        builder.Property(d => d.Scheme_Cat_Edit_Flag).HasColumnName("Scheme_Cat_Edit_Flag");
        builder.Property(d => d.Allow_NegExp_Flag).HasColumnName("Allow_NegExp_Flag");
        builder.Property(d => d.Allow_NegExp_Flag_ddg).HasColumnName("Allow_NegExp_Flag_ddg");
        builder.Property(d => d.Allow_NegRec_Flag).HasColumnName("Allow_NegRec_Flag");
        builder.Property(d => d.Edit_TargetDate).HasColumnName("Edit_TargetDate");
        builder.Property(d => d.DDGTargetDate).HasColumnName("DDGTargetDate");
        builder.Property(d => d.DDGFreez).HasColumnName("DDGFreez");
        builder.Property(d => d.DDGFreezDate).HasColumnName("DDGFreezDate");
        builder.Property(d => d.ConsumedStatus).HasColumnName("ConsumedStatus");
        builder.Property(d => d.DDGUnFreezedDate).HasColumnName("DDGUnFreezedDate");
        builder.Property(d => d.DDGFreezUserId).HasColumnName("DDGFreezUserId");
        builder.Property(d => d.DDGUnFreezeUserId).HasColumnName("DDGUnFreezeUserId");

        builder.HasIndex(d => d.FinancialYear).HasDatabaseName("IX_M_SBEDemand_FinancialYear");
    }
}
