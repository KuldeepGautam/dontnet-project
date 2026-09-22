namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

public class FunctionConfiguration : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> builder)
    {
        builder.ToTable("M_Function", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(f => f.FunctionId);
        builder.Property(f => f.FunctionId).HasColumnName("FunctionId").ValueGeneratedOnAdd();
        builder.Property(f => f.FunctionName).HasColumnName("FunctionName");
        builder.Property(f => f.Active).HasColumnName("Active");
        builder.Property(f => f.ActionSlug).HasColumnName("ActionSlug").HasMaxLength(200);
    }
}
