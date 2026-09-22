namespace UBIS.Services.MobilePhone.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.MobilePhone.Domain.Entities;

public class MobilePhoneDbContext : DbContext
{
    public MobilePhoneDbContext(DbContextOptions<MobilePhoneDbContext> options) : base(options)
    {
    }

    public DbSet<SmsDispatchLog> SmsDispatchLogs => Set<SmsDispatchLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SmsDispatchLog>(builder =>
        {
            builder.ToTable("M_SmsDispatchLog", "dbo", t => t.ExcludeFromMigrations());
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(e => e.MessageId).HasColumnName("MessageId").IsRequired();
            builder.Property(e => e.ToMobileMasked).HasColumnName("ToMobileMasked").HasMaxLength(20).IsRequired();
            builder.Property(e => e.Purpose).HasColumnName("Purpose").HasMaxLength(32).IsRequired();
            builder.Property(e => e.Status).HasColumnName("Status").HasMaxLength(16).IsRequired();
            builder.Property(e => e.ProviderResponseCode).HasColumnName("ProviderResponseCode").HasMaxLength(32).IsRequired(false);
            builder.Property(e => e.SentAtUtc).HasColumnName("SentAtUtc").HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            builder.HasIndex(e => e.MessageId);
        });
    }
}
