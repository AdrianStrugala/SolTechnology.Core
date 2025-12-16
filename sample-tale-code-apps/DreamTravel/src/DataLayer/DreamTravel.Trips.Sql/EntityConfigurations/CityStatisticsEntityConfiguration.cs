using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Trips.Sql.EntityConfigurations;

public class CityStatisticsEntityConfiguration : IEntityTypeConfiguration<CityStatisticsEntity>
{
    public void Configure(EntityTypeBuilder<CityStatisticsEntity> builder)
    {
        builder.ToTable("CityStatistics");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.SearchCount)
            .IsRequired();
        
        builder.HasIndex("CityId");
        
        builder.HasOne(e => e.City)
            .WithMany(c => c.Statistics)
            .HasForeignKey("CityId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}