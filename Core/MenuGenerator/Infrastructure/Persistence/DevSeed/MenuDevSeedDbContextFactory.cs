namespace UBIS.Services.MenuGenerator.Infrastructure.Persistence.DevSeed;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Lets `dotnet ef` create MenuDevSeedDbContext directly at design time, without booting the
/// full WebApi host. Reads "DevSeedConnection" - a deliberately different connection string
/// from production's "DefaultConnection" so this never accidentally targets the shared DB.
/// </summary>
public class MenuDevSeedDbContextFactory : IDesignTimeDbContextFactory<MenuDevSeedDbContext>
{
    public MenuDevSeedDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "WebApi"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MenuDevSeedDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DevSeedConnection"),
            sqlServerOptions => sqlServerOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));

        return new MenuDevSeedDbContext(optionsBuilder.Options);
    }
}
