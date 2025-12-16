using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Trips.Sql.EntityConfigurations;

public class CountryStatisticsEntityConfiguration : IEntityTypeConfiguration<CountryStatisticsEntity>
{
    public void Configure(EntityTypeBuilder<CountryStatisticsEntity> builder)
    {
        builder.ToView("CountryStatisticsView");
        builder.HasNoKey();
    }
}