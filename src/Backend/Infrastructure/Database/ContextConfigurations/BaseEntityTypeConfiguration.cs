using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Infrastructure.Database.ContextConfigurations
{
    public abstract class BaseEntityTypeConfiguration<T> : IEntityTypeConfiguration<T>
        where T : class
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.ToTable(typeof(T).Name);

            builder.Property<int>(BaseEntityFields.Id)
                .UseIdentityColumn(1);

            builder.HasKey(BaseEntityFields.Id);

            builder.Property<DateTime>(BaseEntityFields.CreatedAt);

            builder.Property<DateTime>(BaseEntityFields.ModifiedAt);
        }
    }
}