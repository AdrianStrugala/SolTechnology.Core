using System;
using DreamTravel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Infrastructure.Database.ContextConfigurations
{
    public abstract class AbstractEntityConfiguration<T> : IEntityTypeConfiguration<T>
        where T : AbstractEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.ToTable(typeof(T).Name);

            builder.Property<long>(nameof(AbstractEntity.Id))
                .UseIdentityColumn(1);

            builder.HasKey(nameof(AbstractEntity.Id));

            builder.Property<DateTime>(nameof(AbstractEntity.CreatedAt));

            builder.Property<DateTime>(nameof(AbstractEntity.ModifiedAt));
        }
    }
}