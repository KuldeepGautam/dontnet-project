namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class DemandCeilingConfiguration : IEntityTypeConfiguration<DemandCeiling>
{
    public void Configure(EntityTypeBuilder<DemandCeiling> builder)
    {
        builder.ToTable("M_DemandCeiling", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId).HasColumnName("RowID").ValueGeneratedOnAdd();
        builder.Property(e => e.DemandId).HasColumnName("Demandid");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.BeCeiling).HasColumnName("BE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReCeiling).HasColumnName("RE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NbeCeiling).HasColumnName("NBE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.CapBePer).HasColumnName("Cap_BE_Per").HasColumnType("decimal(6,2)");
        builder.Property(e => e.CapRePer).HasColumnName("Cap_RE_Per").HasColumnType("decimal(6,2)");
        builder.Property(e => e.CapNbePer).HasColumnName("Cap_NBE_Per").HasColumnType("decimal(6,2)");
        builder.Property(e => e.TotSchemeBe).HasColumnName("Tot_Scheme_BE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotSchemeRe).HasColumnName("Tot_Scheme_RE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.TotSchemeNbe).HasColumnName("Tot_Scheme_NBE").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ScPerBe).HasColumnName("SC_Per_BE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.ScPerRe).HasColumnName("SC_Per_RE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.ScPerNbe).HasColumnName("SC_Per_NBE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.StPerBe).HasColumnName("ST_Per_BE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.StPerRe).HasColumnName("ST_Per_RE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.StPerNbe).HasColumnName("ST_Per_NBE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.NerPerBe).HasColumnName("NER_Per_BE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.NerPerRe).HasColumnName("NER_Per_RE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.NerPerNbe).HasColumnName("NER_Per_NBE").HasColumnType("decimal(6,2)");
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate").IsRequired();
        builder.Property(e => e.UserIp).HasColumnName("UserIP").HasMaxLength(15).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserID").IsRequired();

        builder.HasIndex(e => new { e.DemandId, e.FinancialYear }).IsUnique();
    }
}
