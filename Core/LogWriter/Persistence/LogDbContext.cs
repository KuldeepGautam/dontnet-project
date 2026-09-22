namespace UBIS.Services.LogWriter.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.LogWriter.Domain;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<LogEntry> Logs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<LogEntry>(builder =>
        {
            builder.ToTable("M_LogEntry", "dbo", t => t.ExcludeFromMigrations());
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(l => l.Level).HasColumnName("Level").HasMaxLength(50).IsRequired();
            builder.Property(l => l.Message).HasColumnName("Message").HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(l => l.Exception).HasColumnName("Exception").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(l => l.Properties).HasColumnName("Properties").HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(l => l.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

            // Standard audit columns.
            builder.Property(l => l.IsActive).HasColumnName("IsActive");
            builder.Property(l => l.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
            builder.Property(l => l.CreatedOnDate).HasColumnName("CreatedOnDate");
            builder.Property(l => l.UserIdModifyBy).HasColumnName("UserIdModifyBy");
            builder.Property(l => l.ModifiedOnDate).HasColumnName("ModifiedOnDate");
            builder.Property(l => l.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
            builder.Property(l => l.DeletedOnDate).HasColumnName("DeletedOnDate");
        });
    }
}
