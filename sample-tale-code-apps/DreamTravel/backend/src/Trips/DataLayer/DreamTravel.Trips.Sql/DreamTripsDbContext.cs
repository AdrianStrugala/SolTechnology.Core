using DreamTravel.Trips.Domain;
using DreamTravel.Trips.Domain.Cities;
using Microsoft.EntityFrameworkCore;

namespace DreamTravel.Trips.Sql
{

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

        public virtual DbSet<CityDetails> Cities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Data Source=localhost,1403;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CityDetails>(entity =>
            {
                entity.ToTable("City");
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
    }
}
