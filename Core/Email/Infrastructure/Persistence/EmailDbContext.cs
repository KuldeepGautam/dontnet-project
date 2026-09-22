namespace UBIS.Services.Email.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Email.Domain.Entities;

public class EmailDbContext : DbContext
{
    public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
    {
    }

    public DbSet<EmailDispatchLog> EmailDispatchLogs => Set<EmailDispatchLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<EmailDispatchLog>(builder =>
        {
            builder.ToTable("M_EmailDispatchLog", "dbo", t => t.ExcludeFromMigrations());
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(e => e.MessageId).HasColumnName("MessageId").IsRequired();
            builder.Property(e => e.ToEmailMasked).HasColumnName("ToEmailMasked").HasMaxLength(320).IsRequired();
            builder.Property(e => e.Purpose).HasColumnName("Purpose").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Status).HasColumnName("Status").HasMaxLength(16).IsRequired();
            builder.Property(e => e.ProviderResponseCode).HasColumnName("ProviderResponseCode").HasMaxLength(32).IsRequired(false);
            builder.Property(e => e.SentAtUtc).HasColumnName("SentAtUtc").HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.HasIndex(e => e.MessageId);
        });
    }
}
