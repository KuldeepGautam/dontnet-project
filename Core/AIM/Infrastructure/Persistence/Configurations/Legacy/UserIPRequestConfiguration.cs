namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities.Legacy;

/// <summary>EF Core mapping for the legacy dbo.UserIPrequest pass-through. Added 2026-07-10.</summary>
public class UserIPRequestConfiguration : IEntityTypeConfiguration<UserIPRequest>
{
    public void Configure(EntityTypeBuilder<UserIPRequest> builder)
    {
        builder.ToTable("M_UserIPrequest", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(r => r.RowId);
        // Unlike the other legacy pass-throughs (read-only), this application also INSERTs new
        // rows here (Section 3 change-request workflow) — RowId is the legacy table's identity
        // column, so let SQL Server generate it rather than assigning one ourselves.
        builder.Property(r => r.RowId).HasColumnName("RowId").ValueGeneratedOnAdd();
        builder.Property(r => r.UsersId).HasColumnName("UsersId");
        builder.Property(r => r.DemandId).HasColumnName("DemandId");
        builder.Property(r => r.IPadres1).HasColumnName("IPadres1");
        builder.Property(r => r.IPadres2).HasColumnName("IPadres2");
        builder.Property(r => r.RequestDate).HasColumnName("RequestDate");
        builder.Property(r => r.ApproveDate).HasColumnName("ApproveDate");
        builder.Property(r => r.ApproveFlag).HasColumnName("ApproveFlag");
        builder.Property(r => r.ApproveUserId).HasColumnName("ApproveUserId");
        builder.Property(r => r.IP).HasColumnName("IP");
        builder.Property(r => r.Mobile).HasColumnName("Mobile");

        builder.HasIndex(r => r.UsersId).HasDatabaseName("IX_M_UserIPrequest_UsersId");
    }
}
