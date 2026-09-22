namespace UBIS.Services.PreBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.PreBudget.Domain.Entities;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("PreBudgetAuditLog", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(a => a.TableName).HasColumnName("TableName").HasMaxLength(128).IsRequired();
        builder.Property(a => a.RecordId).HasColumnName("RecordId");
        builder.Property(a => a.Action).HasColumnName("Action").HasMaxLength(20).IsRequired();
        builder.Property(a => a.ChangedByUserId).HasColumnName("ChangedByUserId");
        builder.Property(a => a.ChangedByUserName).HasColumnName("ChangedByUserName").HasMaxLength(100);
        builder.Property(a => a.ChangedByIp).HasColumnName("ChangedByIp").HasMaxLength(45);
        builder.Property(a => a.ChangedAtUtc).HasColumnName("ChangedAtUtc");
        builder.Property(a => a.OldValuesJson).HasColumnName("OldValuesJson");
        builder.Property(a => a.NewValuesJson).HasColumnName("NewValuesJson");

        builder.HasIndex(a => new { a.TableName, a.RecordId });
    }
}
