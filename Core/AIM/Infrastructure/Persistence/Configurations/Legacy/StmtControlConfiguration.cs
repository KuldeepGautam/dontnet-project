namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>EF Core mapping for the legacy dbo.M_StmtControl pass-through. Added 2026-07-10.</summary>
public class StmtControlConfiguration : IEntityTypeConfiguration<StmtControl>
{
    public void Configure(EntityTypeBuilder<StmtControl> builder)
    {
        builder.ToTable("M_StmtControl", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(s => s.StmtId);
        builder.Property(s => s.StmtId).HasColumnName("StmtId").ValueGeneratedNever();
        builder.Property(s => s.FinancialYear).HasColumnName("FinancialYear");
        builder.Property(s => s.StmtNo).HasColumnName("StmtNo");
        builder.Property(s => s.StmtName).HasColumnName("StmtName");
        builder.Property(s => s.HStmtName).HasColumnName("HStmtName");
        builder.Property(s => s.HeaderNoteFlag).HasColumnName("HeaderNoteFlag");
        builder.Property(s => s.FooterNoteFlag).HasColumnName("FooterNoteFlag");
        builder.Property(s => s.LayoutId).HasColumnName("LayoutId");
        builder.Property(s => s.LayoutYear).HasColumnName("LayoutYear");
        builder.Property(s => s.Regular_Interim).HasColumnName("Regular_Interim");
        builder.Property(s => s.No_Column).HasColumnName("No_Column");
        builder.Property(s => s.Caption1).HasColumnName("Caption1");
        builder.Property(s => s.Caption2).HasColumnName("Caption2");
        builder.Property(s => s.Caption3).HasColumnName("Caption3");
        builder.Property(s => s.DisplaySeqNo).HasColumnName("DisplaySeqNo");
        builder.Property(s => s.DisplayFigure).HasColumnName("DisplayFigure");
        builder.Property(s => s.TemplateName).HasColumnName("TemplateName");
        builder.Property(s => s.Entrydate).HasColumnName("Entrydate");
        builder.Property(s => s.StatementType).HasColumnName("StatementType");
        builder.Property(s => s.OutputType).HasColumnName("OutputType");
        builder.Property(s => s.PageNo).HasColumnName("PageNo");
        builder.Property(s => s.StmtDisplaySeq).HasColumnName("StmtDisplaySeq");
        builder.Property(s => s.StmtUserId).HasColumnName("StmtUserId");
        builder.Property(s => s.StmtUserId1).HasColumnName("StmtUserId1");
        builder.Property(s => s.PrevStmtId).HasColumnName("PrevStmtId");
        builder.Property(s => s.IsSonata).HasColumnName("IsSonata");
        builder.Property(s => s.IsSSRS).HasColumnName("IsSSRS");

        builder.HasIndex(s => s.StmtUserId).HasDatabaseName("IX_M_StmtControl_StmtUserId");
        builder.HasIndex(s => s.StmtUserId1).HasDatabaseName("IX_M_StmtControl_StmtUserId1");
    }
}
