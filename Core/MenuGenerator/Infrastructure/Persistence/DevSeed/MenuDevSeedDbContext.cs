namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.MenuGenerator.Domain.Entities;
using UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed.Configurations;

/// <summary>
/// Standalone dev/test-only DbContext that owns and fully migrates a self-contained copy of
/// the whole schema (all 6 M_ tables plus M_Users), seeded from the real UBIS_RBAC.xlsx data.
/// This is separate from MenuDbContext (production), which reads the 5 legacy tables
/// read-only and owns only M_App - use this only against a throwaway dev/test database,
/// never against the shared production database.
/// </summary>
public class MenuDevSeedDbContext : DbContext
{
    public MenuDevSeedDbContext(DbContextOptions<MenuDevSeedDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Module> Modules { get; set; } = null!;
    public DbSet<Function> Functions { get; set; } = null!;
    public DbSet<RoleModuleMapping> RoleModuleMappings { get; set; } = null!;
    public DbSet<RoleFunctionMapping> RoleFunctionMappings { get; set; } = null!;
    public DbSet<App> Apps { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleSeedConfiguration());
        modelBuilder.ApplyConfiguration(new ModuleSeedConfiguration());
        modelBuilder.ApplyConfiguration(new FunctionSeedConfiguration());
        modelBuilder.ApplyConfiguration(new RoleModuleMappingSeedConfiguration());
        modelBuilder.ApplyConfiguration(new RoleFunctionMappingSeedConfiguration());
        modelBuilder.ApplyConfiguration(new AppSeedConfiguration());
        modelBuilder.ApplyConfiguration(new UserSeedConfiguration());
    }
}
