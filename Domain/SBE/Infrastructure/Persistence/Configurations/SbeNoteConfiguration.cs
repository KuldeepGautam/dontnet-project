namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class SbeNoteConfiguration : IEntityTypeConfiguration<SbeNote>
{
    public void Configure(EntityTypeBuilder<SbeNote> builder)
    {
        builder.ToTable("SBENotes", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.SbeNoteId);
        builder.Property(e => e.SbeNoteId).HasColumnName("SBENoteId").ValueGeneratedOnAdd();
        builder.Property(e => e.SbeDataId).HasColumnName("SBEDataId");
        builder.Property(e => e.DemandId).HasColumnName("DemandId");
        builder.Property(e => e.DemandNo).HasColumnName("DemandNo");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(10);
        builder.Property(e => e.PrintSequenceNo).HasColumnName("PrintSequenceNo");
        builder.Property(e => e.ParaNo).HasColumnName("ParaNo").HasMaxLength(10);
        builder.Property(e => e.ParaHeading).HasColumnName("ParaHeading").HasMaxLength(500);
        builder.Property(e => e.HParaHeading).HasColumnName("HParaHeading").HasMaxLength(1000);
        builder.Property(e => e.ParaDetails).HasColumnName("ParaDetails");
        builder.Property(e => e.HParaDetails).HasColumnName("HParaDetails");
        builder.Property(e => e.SchemeId).HasColumnName("SchemeId");
        builder.Property(e => e.SubSchemeId).HasColumnName("SubSchemeId");
        builder.Property(e => e.ProgrammeId).HasColumnName("ProgrammeId");
        builder.Property(e => e.SubprogrammeId).HasColumnName("SubprogrammeId");
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate");
        builder.Property(e => e.PrevSbeNoteId).HasColumnName("PrevSBENoteId");
        builder.Property(e => e.UserId).HasColumnName("UserID");
        builder.Property(e => e.Ip).HasColumnName("IP").HasMaxLength(15);

        builder.HasIndex(e => new { e.DemandId, e.FinancialYear });
    }
}
