namespace UBIS.Services.UserProfile.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.UserProfile.Domain.Entities;
using UBIS.Services.UserProfile.Infrastructure.Persistence.Configurations;

/// <summary>
/// DB-first, no EF migrations — every table here is a pre-existing legacy table this service
/// reads/writes, never creates. Same shared BIMS2 database every other microservice in this
/// solution points at.
/// </summary>
public class UserProfileDbContext : DbContext
{
    public UserProfileDbContext(DbContextOptions<UserProfileDbContext> options) : base(options)
    {
    }

    public DbSet<UserIpRequest> IpRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UserIpRequestConfiguration());
    }
}
