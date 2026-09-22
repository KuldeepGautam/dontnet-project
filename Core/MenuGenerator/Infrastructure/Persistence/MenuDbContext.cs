namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.MenuGenerator.Domain.Entities;
using UBIS.Services.MenuGenerator.Infrastructure.Persistence.Configurations;

public class MenuDbContext : DbContext
{
    public MenuDbContext(DbContextOptions<MenuDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Module> Modules { get; set; } = null!;
    public DbSet<Function> Functions { get; set; } = null!;
    public DbSet<RoleModuleMapping> RoleModuleMappings { get; set; } = null!;
    public DbSet<RoleFunctionMapping> RoleFunctionMappings { get; set; } = null!;
    public DbSet<App> Apps { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new ModuleConfiguration());
        modelBuilder.ApplyConfiguration(new FunctionConfiguration());
        modelBuilder.ApplyConfiguration(new RoleModuleMappingConfiguration());
        modelBuilder.ApplyConfiguration(new RoleFunctionMappingConfiguration());
        modelBuilder.ApplyConfiguration(new AppConfiguration());
    }
}
