namespace UBIS.Services.Aim.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UBIS.Services.Aim.Domain.Entities;

public class FunctionConfiguration : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> builder)
    {
        builder.ToTable("M_Function", "dbo", t => t.ExcludeFromMigrations());
        builder.HasKey(f => f.FunctionId);
        builder.Property(f => f.FunctionId).HasColumnName("FunctionId").ValueGeneratedOnAdd();
        builder.Property(f => f.ModuleId).HasColumnName("ModuleId");
        builder.Property(f => f.FunctionName).HasColumnName("FunctionName");
        builder.Property(f => f.AddressOfTheFunction).HasColumnName("AddressOfTheFunction");
        builder.Property(f => f.Remarks).HasColumnName("Remarks");
        builder.Property(f => f.Freez).HasColumnName("Freez");

        builder.HasMany(f => f.RoleFunctionMappings)
            .WithOne(rfm => rfm.Function)
            .HasForeignKey(rfm => rfm.FunctionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
