using DreamTravel.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Sql.EntityConfigurations;

public class CityEntityConfiguration : IEntityTypeConfiguration<CityEntity>
{
    public void Configure(EntityTypeBuilder<CityEntity> builder)
    {
        builder.ToTable("City");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CityId)
            .HasConversion(new AuidValueConverter())
            .IsRequired();
        
        builder.Property(e => e.Latitude)
            .IsRequired();
        
        builder.Property(e => e.Longitude)
            .IsRequired();
        
        builder.Property(e => e.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(e => e.CityId)
            .IsUnique();
    }
}