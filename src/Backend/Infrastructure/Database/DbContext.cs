using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database.ContextConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace DreamTravel.Infrastructure.Database
{
    public class DreamTravelsDbContextFactory : IDesignTimeDbContextFactory<DreamTravelsDbContext>
    {
        public DreamTravelsDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DreamTravelsDbContext>();
            // https://github.com/dotnet/efcore/issues/1470
            optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Database=LicenseDbContextDebug;Integrated Security=True;Persist Security Info=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=True");
            return new DreamTravelsDbContext(optionsBuilder.Options);
        }
    }

    public class DreamTravelsDbContext : DbContext
    {
        public DbSet<User> Contract { get; set; }

        public DreamTravelsDbContext(DbContextOptions<DreamTravelsDbContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            OnBeforeSaving();
            var savedItemsNo = await base.SaveChangesAsync(cancellationToken);

            return savedItemsNo;
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries().ToList();
            foreach (var entry in entries)
            {
                if (entry.Entity is IEntity entity)
                {
                    if (entity.MarkedToDeletion)
                    {
                        entry.State = EntityState.Deleted;
                    }
                }

                var now = DateTime.UtcNow;
                switch (entry.State)
                {
                    case EntityState.Modified:
                        SetValue(entry, BaseEntityFields.ModifiedAt, now);
                        break;

                    case EntityState.Added:
                        SetValue(entry, BaseEntityFields.CreatedAt, now);
                        break;
                }
            }
        }

        private static void SetValue<T>(EntityEntry entry, string fieldName, T value)
        {
            if (entry.CurrentValues.Properties.Any(
                a => string.Equals(a.Name, fieldName, StringComparison.InvariantCultureIgnoreCase)))
            {
                entry.CurrentValues[fieldName] = value;
            }
        }
    }
}
