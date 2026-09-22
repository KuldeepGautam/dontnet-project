namespace UBIS.Services.Sbe.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Sbe.Domain.Entities;

public class ObjectCeilingConfiguration : IEntityTypeConfiguration<ObjectCeiling>
{
    public void Configure(EntityTypeBuilder<ObjectCeiling> builder)
    {
        builder.ToTable("M_ObjectCeiling", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(e => e.RowId);
        builder.Property(e => e.RowId).HasColumnName("RowID").ValueGeneratedOnAdd();
        builder.Property(e => e.DemandId).HasColumnName("DemandID");
        builder.Property(e => e.FinancialYear).HasColumnName("FinancialYear").HasMaxLength(9);
        builder.Property(e => e.ObjectCode).HasColumnName("ObjectCode");
        builder.Property(e => e.BeCeiling).HasColumnName("BE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.ReCeiling).HasColumnName("RE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.NbeCeiling).HasColumnName("NBE_Ceiling").HasColumnType("decimal(18,2)");
        builder.Property(e => e.EntryDate).HasColumnName("EntryDate").IsRequired();
        builder.Property(e => e.UserIp).HasColumnName("UserIP").HasMaxLength(15).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserID").IsRequired();

        builder.HasIndex(e => new { e.DemandId, e.FinancialYear, e.ObjectCode }).IsUnique();
    }
}
