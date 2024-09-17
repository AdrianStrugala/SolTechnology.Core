using Microsoft.EntityFrameworkCore;
using SolTechnology.TaleCode.Domain;
using SolTechnology.TaleCode.SqlData.Repository.ExecutionErrorRepository;

namespace SolTechnology.TaleCode.SqlData
{

    /// <summary>
    /// Scaffolded using:
    /// dotnet-ef dbcontext scaffold "Data Source=localhost,1403;Database=TaleCodeDatabase; User ID=SA;Password=password_xxddd_2137;Persist Security Info=True;MultipleActiveResultSets=True;Trusted_Connection=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context TaleCodeDbContext --force
    /// from SolTechnology.TaleCode.Sql directory
    /// </summary>

    public partial class TaleCodeDbContext : DbContext
    {
        public TaleCodeDbContext()
        {
        }

        public TaleCodeDbContext(DbContextOptions<TaleCodeDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ExecutionError> ExecutionErrors { get; set; }
        public virtual DbSet<Match> Matches { get; set; }
        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<Team> Teams { get; set; }

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
            modelBuilder.Entity<Match>(entity =>
            {
                entity.ToTable("Match");

                entity.HasIndex(e => e.ApiId, "UX_Match_ApiId")
                    .IsUnique();

                entity.Property(e => e.AwayTeam).HasMaxLength(50);

                entity.Property(e => e.CompetitionWinner)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.HomeTeam).HasMaxLength(50);

                entity.Property(e => e.Winner)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Matches)
                    .HasPrincipalKey(p => p.ApiId)
                    .HasForeignKey(d => d.PlayerApiId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Match_Player");
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("Player");

                entity.HasIndex(e => e.ApiId, "UX_Player_ApiId")
                    .IsUnique();

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.Nationality).HasMaxLength(50);

                entity.Property(e => e.Position).HasMaxLength(50);
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("Team");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Teams)
                    .HasPrincipalKey(p => p.ApiId)
                    .HasForeignKey(d => d.PlayerApiId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Team_Player");
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
                entity.ModifiedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
