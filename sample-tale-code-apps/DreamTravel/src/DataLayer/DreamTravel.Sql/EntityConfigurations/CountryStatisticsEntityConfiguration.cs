using DreamTravel.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Sql.EntityConfigurations;

public class CountryStatisticsEntityConfiguration : IEntityTypeConfiguration<CountryStatisticsEntity>
{
    public void Configure(EntityTypeBuilder<CountryStatisticsEntity> builder)
    {
        builder.ToView("CountryStatisticsView");
        builder.HasNoKey();
    }
}