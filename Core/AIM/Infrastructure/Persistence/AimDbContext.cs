namespace UBIS.Services.Aim.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using UBIS.Services.Aim.Domain.Entities;
using UBIS.Services.Aim.Domain.Entities.Legacy;
using UBIS.Services.Aim.Infrastructure.Persistence.Configurations;
using UBIS.Services.Aim.Infrastructure.Persistence.Configurations.Legacy;

/// <summary>
/// Entity Framework Core DbContext for the AIM (Authentication & Identity Management) microservice.
/// Every entity lives in the real, DBA-owned <c>dbo</c> schema as of 2026-07-13 (no more <c>aim</c>
/// schema) — this context is purely a typed query/mapping layer, not a schema owner; every table
/// is <c>ExcludeFromMigrations()</c> (there are no EF migrations anywhere in this solution).
/// </summary>
public class AimDbContext : DbContext
{
    public AimDbContext(DbContextOptions<AimDbContext> options) : base(options)
    {
    }

    /// <summary>DbSet for User entities (dbo.M_Users).</summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>DbSet for Role entities (dbo.M_Role).</summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>DbSet for Function entities (dbo.M_Function).</summary>
    public DbSet<Function> Functions { get; set; } = null!;

    /// <summary>DbSet for RoleFunctionMapping entities (dbo.M_MapRoleFunction, renamed 2026-08-04 from M_RoleFunctionMapping).</summary>
    public DbSet<RoleFunctionMapping> RoleFunctionMappings { get; set; } = null!;

    /// <summary>DbSet for SecurityEvent entities (immutable audit trail, dbo.M_SecurityEvent).</summary>
    public DbSet<SecurityEvent> SecurityEvents { get; set; } = null!;

    /// <summary>DbSet for PasswordHistory entities (dbo.M_PasswordHistory).</summary>
    public DbSet<PasswordHistory> PasswordHistories { get; set; } = null!;

    /// <summary>DbSet for MapUserRole entities (dbo.M_MapUserRole, renamed 2026-08-04 from
    /// M_MapUserDemandFY, itself renamed 2026-07-13 from M_UserCharges) — authoritative Role
    /// assignment per user, added 2026-07-13, consolidated to one row per user 2026-08-04.</summary>
    public DbSet<MapUserRole> MapUserRoles { get; set; } = null!;

    /// <summary>DbSet for UserDemandMapping entities (dbo.M_MapUserDemand, renamed 2026-08-04 from
    /// M_UserDemandMapping) — multi-Demand access list per user, added 2026-07-13.</summary>
    public DbSet<UserDemandMapping> UserDemandMappings { get; set; } = null!;

    /// <summary>DbSet for MapUserDepartment entities (dbo.M_MapUserDepartment) — department
    /// assignment per user, extracted from M_User.DepartmentId 2026-08-17.</summary>
    public DbSet<MapUserDepartment> MapUserDepartments { get; set; } = null!;

    /// <summary>DbSet for MapUserIPAddress entities (dbo.M_MapUserIPAddress) — allowed login IPs
    /// per user, extracted from M_User.IPadres1/IPadres2/IPAuthFlag 2026-08-17.</summary>
    public DbSet<MapUserIPAddress> MapUserIPAddresses { get; set; } = null!;

    /// <summary>DbSet for MapUserApp entities (dbo.M_MapUserApp) — normalized replacement for the
    /// old comma-separated M_User.AppId column, added 2026-08-03.</summary>
    public DbSet<MapUserApp> MapUserApps { get; set; } = null!;

    /// <summary>DbSet for UserFinancialYear entities (dbo.M_MapUserFY) — holds the per-year
    /// eligibility association that used to live directly on M_User.FinancialYear, added
    /// 2026-08-03 when M_User was consolidated to one row per person.</summary>
    public DbSet<UserFinancialYear> UserFinancialYears { get; set; } = null!;

    /// <summary>DbSet for Demand entities (dbo.M_Demand) — backs GET /api/users/demands, added 2026-07-23.</summary>
    public DbSet<Demand> Demands { get; set; } = null!;

    /// <summary>DbSet for FinancialYear entities (dbo.M_FinancialYear) — backs GET /api/authentication/financial-years, added 2026-07-30.</summary>
    public DbSet<FinancialYear> FinancialYears { get; set; } = null!;

    // ------------------------------------------------------------------
    // Legacy dbo-schema pass-through DbSets (read/write against pre-existing BIMS tables;
    // physical schema untouched — see UBIS.Services.Aim.Domain.Entities.Legacy). Added 2026-07-10.
    // ------------------------------------------------------------------

    /// <summary>DbSet for the legacy dbo.UserDetails (User-to-Demand "Profile Access") pass-through.</summary>
    public DbSet<UserDetails> UserDetails { get; set; } = null!;

    /// <summary>DbSet for the legacy dbo.Section lookup pass-through.</summary>
    public DbSet<Section> Sections { get; set; } = null!;

    /// <summary>DbSet for the legacy dbo.M_StmtControl (Statement ownership) pass-through.</summary>
    public DbSet<StmtControl> StmtControls { get; set; } = null!;

    /// <summary>DbSet for the legacy dbo.M_SBEDemand pass-through.</summary>
    public DbSet<SBEDemand> SBEDemands { get; set; } = null!;

    /// <summary>DbSet for the legacy dbo.UserIPrequest pass-through.</summary>
    public DbSet<UserIPRequest> UserIPRequests { get; set; } = null!;

    /// <summary>DbSet for the new dbo.UserId_StmtId_Mapping (non-owner Statement/"Profile" assignment) pass-through.</summary>
    public DbSet<UserStmtMapping> UserStmtMappings { get; set; } = null!;

    /// <summary>
    /// Configures the database context options.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=DESKTOP-5D5JITM\\SQLEXPRESS;Database=BIMS2;Encrypt=true;TrustServerCertificate=true;Connection Timeout=30;");
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Configures model entities, relationships, and constraints using Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new FunctionConfiguration());
        modelBuilder.ApplyConfiguration(new RoleFunctionMappingConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityEventConfiguration());
        modelBuilder.ApplyConfiguration(new PasswordHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new MapUserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserDemandMappingConfiguration());
        modelBuilder.ApplyConfiguration(new MapUserDepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new MapUserIPAddressConfiguration());
        modelBuilder.ApplyConfiguration(new MapUserAppConfiguration());
        modelBuilder.ApplyConfiguration(new UserFinancialYearConfiguration());
        modelBuilder.ApplyConfiguration(new DemandConfiguration());
        modelBuilder.ApplyConfiguration(new FinancialYearConfiguration());

        // Legacy dbo-schema pass-throughs (added 2026-07-10)
        modelBuilder.ApplyConfiguration(new UserDetailsConfiguration());
        modelBuilder.ApplyConfiguration(new SectionConfiguration());
        modelBuilder.ApplyConfiguration(new StmtControlConfiguration());
        modelBuilder.ApplyConfiguration(new SBEDemandConfiguration());
        modelBuilder.ApplyConfiguration(new UserIPRequestConfiguration());
        modelBuilder.ApplyConfiguration(new UserStmtMappingConfiguration());
    }

    /// <summary>
    /// Overrides SaveChanges to enforce business rules and audit requirements.
    /// - Prevents modification of SecurityEvent entities (immutability)
    /// - Stamps CreatedOnDate/ModifiedOnDate audit columns
    /// </summary>
    public override int SaveChanges()
    {
        EnforceAuditRules();
        EnforceSecurityEventImmutability();
        return base.SaveChanges();
    }

    /// <summary>
    /// Overrides SaveChangesAsync to enforce business rules and audit requirements asynchronously.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceAuditRules();
        EnforceSecurityEventImmutability();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps the standard audit date columns (CreatedOnDate/ModifiedOnDate) automatically.
    /// UserIdCreatedBy/UserIdModifyBy are left to calling code, since the acting user isn't
    /// knowable from SaveChanges alone.
    /// </summary>
    private void EnforceAuditRules()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity is User user)
            {
                user.CreatedOnDate = now;
            }
            else if (entry.Entity is SecurityEvent se)
            {
                se.Timestamp = now;
                se.CreatedOnDate = now;
            }
            else if (entry.Entity is PasswordHistory ph)
            {
                ph.CreatedAtUtc = now;
                ph.CreatedOnDate = now;
            }
            else if (entry.Entity is MapUserRole mudfyAdded)
            {
                mudfyAdded.CreatedAt = now;
                mudfyAdded.UpdatedAt = now;
            }
            else if (entry.Entity is MapUserApp mappedAppAdded)
            {
                mappedAppAdded.CreatedAt = now;
                mappedAppAdded.UpdatedAt = now;
            }
            else if (entry.Entity is UserFinancialYear userFyAdded)
            {
                userFyAdded.CreatedAt = now;
                userFyAdded.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified))
        {
            if (entry.Entity is User user)
            {
                user.ModifiedOnDate = now;
            }
            else if (entry.Entity is MapUserRole mudfyModified)
            {
                mudfyModified.UpdatedAt = now;
            }
            else if (entry.Entity is MapUserApp mappedAppModified)
            {
                mappedAppModified.UpdatedAt = now;
            }
            else if (entry.Entity is UserFinancialYear userFyModified)
            {
                userFyModified.UpdatedAt = now;
            }
        }
    }

    /// <summary>
    /// Enforces immutability of SecurityEvent entities (insert-only policy).
    /// Throws an exception if an attempt is made to update or delete a security event.
    /// </summary>
    private void EnforceSecurityEventImmutability()
    {
        var modifiedSecurityEvents = ChangeTracker.Entries<SecurityEvent>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (modifiedSecurityEvents.Count > 0)
        {
            throw new InvalidOperationException(
                "SecurityEvents table is immutable. Insert-only operations are allowed. " +
                "Modification or deletion of security events is not permitted.");
        }
    }
}
