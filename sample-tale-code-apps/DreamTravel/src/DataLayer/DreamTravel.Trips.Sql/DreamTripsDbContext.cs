using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DreamTravel.Trips.Sql;

public partial class DreamTripsDbContext : DbContext
{
    public DreamTripsDbContext()
    {
    }

    public DreamTripsDbContext(DbContextOptions<DreamTripsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CityEntity> Cities { get; set; }
    public virtual DbSet<CityStatisticsEntity> CityStatistics { get; set; }
    public virtual DbSet<CountryStatisticsEntity> CountryStatistics { get; set; }
    public virtual DbSet<AlternativeNameEntity> CityAlternativeNames { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: BaseEntity, State: EntityState.Added or EntityState.Modified })
            .ToList();
            
        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }

    public override void Dispose()
    {
        SaveChanges();
        base.Dispose();
    }
}