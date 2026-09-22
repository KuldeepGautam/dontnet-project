namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        // Server DB carries TR_Audit_dbo_M_SecurityEvents (not present locally) - SQL Server
        // rejects the OUTPUT clause EF Core's SqlServer provider otherwise uses to read back
        // store-generated values (EventId, the Timestamp default) on INSERT against a table with
        // an AFTER trigger. Disabling it makes EF fall back to a plain follow-up SELECT instead -
        // see https://aka.ms/efcore-docs-sqlserver-save-changes-and-output-clause.
        builder.ToTable("M_SecurityEvents", "dbo", t => t.ExcludeFromMigrations().UseSqlOutputClause(false));

        builder.HasKey(se => se.EventId);
        builder.Property(se => se.EventId).HasColumnName("EventId").ValueGeneratedOnAdd();

        builder.Property(se => se.EventType)
            .HasColumnName("EventType")
            .HasColumnType("nvarchar(100)")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(se => se.EventType).HasDatabaseName("IX_M_SecurityEvent_EventType");

        builder.Property(se => se.UserId).HasColumnName("UserId");
        builder.HasIndex(se => se.UserId).HasDatabaseName("IX_M_SecurityEvent_UserId");

        builder.Property(se => se.IpAddress)
            .HasColumnName("IPAddress")
            .HasColumnType("nvarchar(45)")
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(se => se.Timestamp)
            .HasColumnName("Timestamp")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(se => se.Timestamp).HasDatabaseName("IX_M_SecurityEvent_Timestamp");

        builder.Property(se => se.Detail).HasColumnName("Detail").HasColumnType("nvarchar(max)");

        // Standard audit columns.
        builder.Property(se => se.IsActive).HasColumnName("IsActive");
        builder.Property(se => se.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(se => se.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(se => se.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(se => se.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(se => se.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(se => se.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasOne(se => se.User)
            .WithMany(u => u.SecurityEvents)
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
