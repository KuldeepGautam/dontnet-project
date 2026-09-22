namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.MenuGenerator.Domain.Entities;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("M_Module", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(m => m.ModuleId);
        builder.Property(m => m.ModuleId).HasColumnName("ModuleId").ValueGeneratedOnAdd();
        builder.Property(m => m.ModuleName).HasColumnName("ModuleName");
        builder.Property(m => m.Active).HasColumnName("Active");
        builder.Property(m => m.ControllerSlug).HasColumnName("ControllerSlug").HasMaxLength(200);
    }
}
