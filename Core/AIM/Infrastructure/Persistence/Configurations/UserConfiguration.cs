namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Server DB carries TR_Audit_dbo_M_User (not present locally) - SQL Server rejects the
        // OUTPUT clause EF Core's SqlServer provider otherwise uses to read back store-generated
        // values on INSERT/UPDATE against a table with an AFTER trigger. Disabling it makes EF
        // fall back to a plain follow-up SELECT instead - see
        // https://aka.ms/efcore-docs-sqlserver-save-changes-and-output-clause.
        builder.ToTable("M_User", "dbo", t => t.ExcludeFromMigrations().UseSqlOutputClause(false));

        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).HasColumnName("UserId").ValueGeneratedOnAdd();

        builder.Property(u => u.Username).HasColumnName("Username").IsRequired();
        builder.HasIndex(u => u.Username).HasDatabaseName("IX_M_Users_LoginId");

        builder.Property(u => u.PasswordHash).HasColumnName("Password");
        builder.Property(u => u.LastLoginDate).HasColumnName("LastLoginDate");
        builder.Property(u => u.LastPasswordChangeDate).HasColumnName("LastPasswordChangeDate");
        builder.Property(u => u.FirstFailLoginAttemptDate).HasColumnName("FirstFailLoginAttemptDate");
        builder.Property(u => u.FailedLoginAttempts).HasColumnName("FailedLoginAttempts");
        builder.Property(u => u.OldPassword1).HasColumnName("OldPassword1");
        builder.Property(u => u.OldPassword2).HasColumnName("OldPassword2");
        builder.Property(u => u.OldPassword3).HasColumnName("OldPassword3");
        builder.Property(u => u.InitialUserLevel).HasColumnName("InitialUserLevel");
        // Replaces plaintext Mobile 2026-08-17 (Workstream 7) — see EncryptedMobile's doc comment.
        builder.Property(u => u.EncryptedMobile).HasColumnName("EncryptedMobile");
        builder.Property(u => u.Email).HasColumnName("Email");
        builder.Property(u => u.ContactPerson).HasColumnName("ContactPerson");
        builder.Property(u => u.AlternateEmail).HasColumnName("AlternateEmail");
        builder.Property(u => u.OTP).HasColumnName("OTP");
        builder.Property(u => u.TransactionDate).HasColumnName("TransactionDate");
        builder.Property(u => u.OTPAttempt).HasColumnName("OTPAttempt");
        builder.Property(u => u.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(u => u.IsLocked).HasColumnName("IsLocked").IsRequired();
        builder.Property(u => u.FreezedFunctionIds).HasColumnName("FreezedFunctionIds");
        builder.Property(u => u.LastEmailChangeDate).HasColumnName("LastEmailChangeDate");
        builder.Property(u => u.EntryDate).HasColumnName("EntryDate");

        // Compliance-brief columns added via db-scripts/M_Users_ComplianceColumns.sql.
        builder.Property(u => u.PasswordResetRequired).HasColumnName("PasswordResetRequired");
        builder.Property(u => u.EmailLastValidatedAtUtc).HasColumnName("EmailLastValidatedAtUtc");

        // Standard audit columns.
        builder.Property(u => u.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(u => u.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(u => u.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(u => u.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(u => u.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(u => u.DeletedOnDate).HasColumnName("DeletedOnDate");

        // Computed, not a real column.
        builder.Ignore(u => u.FullName);

        builder.HasMany(u => u.SecurityEvents)
            .WithOne(se => se.User)
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.PasswordHistories)
            .WithOne(ph => ph.User)
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MapUserApps)
            .WithOne(ma => ma.User)
            .HasForeignKey(ma => ma.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(u => u.UserFinancialYears)
            .WithOne(fy => fy.User)
            .HasForeignKey(fy => fy.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
