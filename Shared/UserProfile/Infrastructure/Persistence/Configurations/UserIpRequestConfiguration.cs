namespace UBIS.Services.UserProfile.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.UserProfile.Domain.Entities;

/// <summary>
/// Maps to the pre-existing legacy dbo.M_UserIPrequest table (read/write) — confirmed against
/// the DBA's real export (new-tables/M_UserIPrequest.sql). No EF migrations, DB-first like every
/// other service in this solution.
/// </summary>
public class UserIpRequestConfiguration : IEntityTypeConfiguration<UserIpRequest>
{
    public void Configure(EntityTypeBuilder<UserIpRequest> builder)
    {
        builder.ToTable("M_UserIPrequest", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(r => r.RowId);
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
        builder.Property(r => r.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(r => r.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(r => r.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(r => r.ModifiedOnDate).HasColumnName("ModifiedOnDate");
        builder.Property(r => r.UserIdDeletedBy).HasColumnName("UserIdDeletedBy");
        builder.Property(r => r.DeletedOnDate).HasColumnName("DeletedOnDate");

        builder.HasIndex(r => r.UsersId).HasDatabaseName("IX_M_UserIPrequest_UsersId_UserProfile");
    }
}
