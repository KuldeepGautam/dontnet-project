namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class MapUserDepartmentConfiguration : IEntityTypeConfiguration<MapUserDepartment>
{
    public void Configure(EntityTypeBuilder<MapUserDepartment> builder)
    {
        builder.ToTable("M_MapUserDepartment", "dbo", t => t.ExcludeFromMigrations());

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedOnAdd();
        builder.Property(m => m.UserId).HasColumnName("UserId");
        builder.Property(m => m.DepartmentId).HasColumnName("DepartmentId");
        builder.Property(m => m.UserIdCreatedBy).HasColumnName("UserIdCreatedBy");
        builder.Property(m => m.CreatedOnDate).HasColumnName("CreatedOnDate");
        builder.Property(m => m.UserIdModifyBy).HasColumnName("UserIdModifyBy");
        builder.Property(m => m.ModifiedOnDate).HasColumnName("ModifiedOnDate");

        builder.HasIndex(m => m.UserId).HasDatabaseName("IX_M_MapUserDepartment_UserId").IsUnique();
    }
}
