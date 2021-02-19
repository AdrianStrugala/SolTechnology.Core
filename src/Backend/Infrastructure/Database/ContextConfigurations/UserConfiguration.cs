using DreamTravel.Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DreamTravel.Infrastructure.Database.ContextConfigurations
{
    public class UserConfiguration : AbstractEntityConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Ignore(i => i.MarkedToDeletion);

            builder.Property(p => p.UserId).IsRequired();
            builder.Property(p => p.Email).IsRequired();
            builder.Property(p => p.Name).IsRequired();
            builder.Property(p => p.Password).IsRequired();

            base.Configure(builder);
        }
    }
}