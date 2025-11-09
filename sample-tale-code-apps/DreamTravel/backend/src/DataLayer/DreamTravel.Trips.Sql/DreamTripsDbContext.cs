using DreamTravel.Trips.Domain;
using DreamTravel.Trips.Domain.Cities;
using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Sql;

/// <summary>
/// Scaffolded using:
/// dotnet-ef dbcontext scaffold "Data Source=localhost,1403;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context DreamTripsDbContext --force
/// from SolTechnology.TaleCode.Sql directory
/// </summary>
public partial class DreamTripsDbContext : DbContext
{
    public DreamTripsDbContext()
    {
    }

    public DreamTripsDbContext(DbContextOptions<DreamTripsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CityDbModel> Cities { get; set; }
    public virtual DbSet<CityStatisticsDbModel> CityStatistics { get; set; }
    public virtual DbSet<CountryStatisticsDbModel> CountryStatistics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CityDbModel>(entity =>
        {
            entity.ToTable("City");
        });
            
        modelBuilder.Entity<CountryStatisticsDbModel>(entity =>
        {
            entity.ToView("CountryStatisticsView");   // view name in db
            entity.HasNoKey();                        // keyless, because it's view
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
            .Where(e => e is { Entity: EntityBase, State: EntityState.Added or EntityState.Modified })
            .ToList();
        foreach (var entry in entries)
        {
            var entity = (EntityBase)entry.Entity;
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