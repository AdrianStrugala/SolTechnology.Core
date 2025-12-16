using DreamTravel.Trips.Sql.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Trips.Sql.EntityConfigurations;

public class AlternativeNameEntityConfiguration : IEntityTypeConfiguration<AlternativeNameEntity>
{
    public void Configure(EntityTypeBuilder<AlternativeNameEntity> builder)
    {
        builder.ToTable("CityAlternativeName");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AlternativeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(e => e.AlternativeName);
        builder.HasIndex("CityId");
        
        builder.HasOne(e => e.City)
            .WithMany(c => c.AlternativeNames)
            .HasForeignKey("CityId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}