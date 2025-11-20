using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

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
        modelBuilder.Entity<CityEntity>(entity =>
        {
            entity.ToTable("City");
            entity.HasKey(e => e.Id);
        
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100);
    
            entity.HasMany(c => c.AlternativeNames)
                .WithOne(a => a.City)
                .HasForeignKey(a => a.CityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<CityStatisticsEntity>()
                .WithOne(s => s.City)
                .HasForeignKey(s => s.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlternativeNameEntity>(entity =>
        {
            entity.ToTable("CityAlternativeName");
            entity.HasKey(e => e.Id);
        
            entity.Property(e => e.AlternativeName)
                .IsRequired()
                .HasMaxLength(200);
        
            entity.HasIndex(e => e.AlternativeName); // Index dla szybszego wyszukiwania
            entity.HasIndex(e => e.CityId);
        });
    
        modelBuilder.Entity<CityStatisticsEntity>(entity =>
        {
            entity.ToTable("CityStatistics");
            entity.HasKey(e => e.Id);
        
            // Możesz dodać composite unique index na CityId + Date
            entity.HasIndex(e => new { e.CityId, e.Date }).IsUnique();
        });

        modelBuilder.Entity<CountryStatisticsEntity>(entity =>
        {
            entity.ToView("CountryStatisticsView");
            entity.HasNoKey(); // keyless, because it's view
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

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