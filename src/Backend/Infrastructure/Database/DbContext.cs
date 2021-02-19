using System;
using DreamTravel.Domain.Users;
using DreamTravel.Infrastructure.Database.ContextConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DreamTravel.Infrastructure.Database
{
    //This class is an entry point for EF migrations
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
        public DbSet<User> Users { get; set; }

        public DreamTravelsDbContext(DbContextOptions<DreamTravelsDbContext> options)
            : base(options)
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }
}
